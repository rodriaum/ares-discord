using Ares.Database.Model;
using Ares.Database.Mongo;
using Ares.Database.Redis;
using Ares.Manager;
using Ares.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Ares.Database.Collection;

/// <summary>
/// Class responsible for managing guild data in MongoDB database.
/// </summary>
internal class GuildCollection
{
    /// <summary>
    /// Represents the "guilds" collection in MongoDB database.
    /// </summary>
    private readonly IMongoCollection<BsonDocument>? _collection;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisDatabase _redisDatabase;

    /// <summary>
    /// Reference to the guild manager used for caching operations and related logic.
    /// </summary>
    private readonly GuildManager _manager;

    /// <summary>
    /// Key prefix used for guild data in Redis.
    /// </summary>
    private readonly String GRedisKey = "guild:";

    /*
     * Constructors and initialization methods.
     */

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildCollection"/> class with the guilds collection and guild manager.
    /// </summary>
    /// <param name="mongoDatabase">MongoDB database instance that contains the "guilds" collection.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public GuildCollection(MongoDatabase mongoDatabase, RedisDatabase redisDatabase)
    {
        _collection = mongoDatabase.mongoDatabase?.GetCollection<BsonDocument>("guilds");
        this._redisDatabase = redisDatabase;

        _manager = Program.GuildManager;

        // Create indexes in the collection to optimize queries.
        CreateIndexesAsync();
    }

    /// <summary>
    /// Attempts to establish a connection with MongoDB, checking the connection every 15 seconds
    /// if the connection fails. The function will continue trying until the connection is successful.
    /// </summary>
    /// <returns>Returns true when the connection to MongoDB is successfully established.</returns>
    public async Task<bool> WaitForMongoConnectionAsync()
    {
        var isConnected = false;

        while (!isConnected)
        {
            try
            {
                // Try to send a ping command to verify the connection.
                if (_collection == null) continue;
                await _collection.Database.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }");
                isConnected = true;
            }
            catch (Exception ex)
            {
                await AresLogger.ErrorAsync("ConnectionError", $"Failed to connect to MongoDB. Retrying in 15 seconds...", ex.Message);
                await Task.Delay(15000);
            }
        }

        return isConnected;
    }

    /// <summary>
    /// Creates indexes in the "guilds" collection to improve query performance.
    /// </summary>
    public async void CreateIndexesAsync()
    {
        await AresLogger.LogAsync("DB: Mongo", "Creating indexes in the database...");

        // Check if the collection was initialized before trying to create indexes.
        if (_collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when creating guild data indexes.");
            return;
        }

        // Call the function to wait for MongoDB connection.
        bool isConnected = await WaitForMongoConnectionAsync();

        if (isConnected)
        {
            // After the connection is successful, create the indexes.
            try
            {
                var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Id");
                var indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

                await _collection.Indexes.CreateManyAsync(new List<CreateIndexModel<BsonDocument>> { indexModel });

                await AresLogger.LogAsync("DB: Mongo", "Indexes created.");
            }
            catch (Exception ex)
            {
                await AresLogger.ErrorAsync("IndexCreationError", $"Error creating indexes: {ex.Message}");
            }
        }
    }

    /*
     * Database operations.
     */

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Guild?> SaveAsync(string id)
    {
        if (_collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when save guild data.");
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
        var element = await _collection.Find(filter).FirstOrDefaultAsync();

        Guild? guild = new Guild(id);

        if (element != null)
        {
            guild = await DeserializeGuildAsync(element) ?? guild;
        }
        else
        {
            string guildJson = await SerializeGuildAsync(guild);
            BsonDocument document = BsonDocument.Parse(guildJson);

            // Insert the document in the database if it doesn't exist.
            await _collection.InsertOneAsync(document);

            _redisDatabase.Save(GRedisKey + id, guild);
            _manager.Save(guild);
        }

        return guild;
    }

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Ulong of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Guild?> SaveAsync(ulong id)
    {
        return await SaveAsync(id.ToString());
    }

    /// <summary>
    /// Retrieves a guild from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the retrieved guild, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(ulong, bool)"/>
    public async Task<Model.Guild?> FetchAsync(string id, bool saveInRedis = false)
    {
        Model.Guild? guild = _manager.Fetch(id);

        if (guild == null)
        {
            guild = _redisDatabase.Load<Guild>(GRedisKey + id);

            if (guild == null)
            {
                BsonDocument element = await _collection.Find(Builders<BsonDocument>.Filter.Eq("Id", id)).FirstOrDefaultAsync();

                if (element != null)
                {
                    guild = await DeserializeGuildAsync(element);

                    if (saveInRedis && guild != null)
                    {
                        _redisDatabase.Save(GRedisKey + id, guild);
                    }
                }
            }
        }

        return guild;
    }

    /// <summary>
    /// Overload of the Fetch method that accepts a ulong numeric ID.
    /// </summary>
    /// <param name="id">Numeric ID of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the retrieved guild, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(string, bool)"/>
    public async Task<Model.Guild?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        return await FetchAsync(id.ToString(), saveInRedis);
    }

    /// <summary>
    /// Updates a specific field of a guild in the database.
    /// </summary>
    /// <param name="guild">A <see cref="Model.Guild"/> object representing the guild to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(Guild guild, string field)
    {
        if (_collection == null) return false;

        try
        {
            string serialized = await SerializeGuildAsync(guild);
            BsonDocument tree = BsonDocument.Parse(serialized);

            if (!tree.TryGetValue(field, out var value))
                value = null;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Id", guild.Id);

            UpdateDefinition<BsonDocument> update = value != null
                ? Builders<BsonDocument>.Update.Set(field, value)
                : Builders<BsonDocument>.Update.Unset(field);

            await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);

            // Update Redis
            _redisDatabase.Update(GRedisKey + guild.Id, guild);

            return true;
        }
        catch (Exception e)
        {
            await AresLogger.ErrorAsync(e.Source ?? "Exception", "Unable to update guild data.", e.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes a guild from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the guild to be removed from the cache.</param>
    public void DeleteCache(string id)
    {
        _redisDatabase.Cache(GRedisKey + id, 300);
        _manager?.Delete(id);
    }

    /// <summary>
    /// Removes a guild from the local cache.
    /// </summary>
    /// <param name="id">Ulong of the guild to be removed from the cache.</param>
    public void DeleteCache(ulong id)
    {
        DeleteCache(id.ToString());
    }

    /// <summary>
    /// Removes the expiration time from the specified key, making it persistent.
    /// </summary>
    /// <param name="id">The unique identifier for the key to be persisted.</param>
    public void Persist(string id)
    {
        _redisDatabase.Persist(GRedisKey + id);
    }

    /// <summary>
    /// Retrieves all guilds from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved guilds.</returns>
    public async Task<ConcurrentBag<Model.Guild>> GetGuildsAsync(int limit = 0)
    {
        var accounts = new ConcurrentBag<Model.Guild>();

        if (_collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when get all guilds.");
            return accounts;
        }

        var options = new FindOptions<BsonDocument> { Limit = limit };
        var documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                // Convert the BSON document to JSON and deserialize to the Guild object.
                var json = document.ToJson();
                var bsonDocument = BsonTypeMapper.MapToDotNetValue(document);
                var jsonString = JsonConvert.SerializeObject(bsonDocument);
                var guild = JsonConvert.DeserializeObject<Model.Guild>(jsonString);

                if (guild != null)
                    accounts.Add(guild);
            }
            catch (JsonReaderException ex)
            {
                await AresLogger.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
            }
        });

        return accounts;
    }

    /*
     * Serialization and deserialization methods.
     */

    /// <summary>
    /// Serializes a guild object to a JSON string.
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    private async Task<string> SerializeGuildAsync(Guild guild)
    {
        return await Task.Run(() => JsonConvert.SerializeObject(guild));
    }

    /// <summary>
    /// Deserializes a BSON document to a guild object.
    /// </summary>
    /// <param name="bsonDoc"></param>
    /// <returns></returns>
    private async Task<Guild?> DeserializeGuildAsync(BsonDocument bsonDoc)
    {
        return await Task.Run(() =>
        {
            try
            {
                object mappedDoc = BsonTypeMapper.MapToDotNetValue(bsonDoc);
                string json = JsonConvert.SerializeObject(mappedDoc);

                return JsonConvert.DeserializeObject<Guild>(json);
            }
            catch (JsonReaderException ex)
            {
                AresLogger.Error("JsonReaderException", "Error deserializing guild document.", ex.Message);
                return null;
            }
        });
    }
}