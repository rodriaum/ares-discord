using Ares.src.Database.Model;
using Ares.src.Database.Mongo;
using Ares.src.Manager;
using Ares.src.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

using System.Collections.Concurrent;

namespace Ares.src.Database.Collection;

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
    /// Reference to the guild manager used for caching operations and related logic.
    /// </summary>
    private readonly GuildManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildCollection"/> class with the guilds collection and guild manager.
    /// </summary>
    /// <param name="database">MongoDB database instance that contains the "guilds" collection.</param>
    public GuildCollection(MongoDatabase database)
    {
        _collection = database.mongoDatabase?.GetCollection<BsonDocument>("guilds");
        _manager = Program.GuildManager;

        // Create indexes in the collection to optimize queries.
        CreateIndexes();
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
                AresLogger.Error("ConnectionError", $"Failed to connect to MongoDB. Retrying in 15 seconds...", ex.Message);
                await Task.Delay(15000);
            }
        }

        return isConnected;
    }

    /// <summary>
    /// Creates indexes in the "guilds" collection to improve query performance.
    /// </summary>
    public async void CreateIndexes()
    {
        await AresLogger.LogAsync("MongoDB", "Creating indexes in the database...");

        // Check if the collection was initialized before trying to create indexes.
        if (_collection == null)
        {
            AresLogger.Error("CollectionNull", "Collection returned null when creating guild data indexes.");
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

                await AresLogger.LogAsync("MongoDB", "Indexes created.");
            }
            catch (Exception ex)
            {
                AresLogger.Error("IndexCreationError", $"Error creating indexes: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Model.Guild?> Save(string id)
    {
        if (_collection == null)
        {
            AresLogger.Error("CollectionNull", "Collection returned null when save guild data.");
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
        var element = await _collection.Find(filter).FirstOrDefaultAsync();

        Model.Guild? guild = new Guild(id);

        if (element != null)
        {
            try
            {
                // Convert the BSON document to JSON and deserialize to the Guild object.
                var document = BsonTypeMapper.MapToDotNetValue(element);
                var json = JsonConvert.SerializeObject(document);

                guild = JsonConvert.DeserializeObject<Model.Guild>(json);
            }
            catch (JsonReaderException ex)
            {
                await AresLogger.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
            }
        }
        else
        {
            // Insert the document in the database if it doesn't exist.
            var document = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
            await _collection.InsertOneAsync(document);

            _manager.Save(guild);
        }

        return guild;
    }

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Ulong of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Model.Guild?> Save(ulong id)
    {
        return await Save(id.ToString());
    }

    /// <summary>
    /// Retrieves a guild from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Model.Guild"/> object representing the retrieved guild, or null if not found.</returns>
    public async Task<Model.Guild?> Fetch(string id)
    {
        Model.Guild? guild = _manager.Fetch(id);

        if (guild == null)
        {
            BsonDocument element = await _collection.Find(Builders<BsonDocument>.Filter.Eq("Id", id)).FirstOrDefaultAsync();

            if (element != null)
            {
                try
                {
                    // Convert the BSON document to JSON and deserialize to the Guild object.
                    var document = BsonTypeMapper.MapToDotNetValue(element);
                    var json = JsonConvert.SerializeObject(document);

                    guild = JsonConvert.DeserializeObject<Model.Guild>(json);
                }
                catch (JsonReaderException ex)
                {
                    await AresLogger.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
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
    public async Task<Model.Guild?> Fetch(ulong id)
    {
        return await Fetch(id.ToString());
    }

    /// <summary>
    /// Updates a specific field of a guild in the database.
    /// </summary>
    /// <param name="guild">A <see cref="Model.Guild"/> object representing the guild to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> Update(Model.Guild guild, string field)
    {
        if (_collection == null)
        {
            AresLogger.Error("CollectionNull", "Collection returned null when update guild data.");
            return false;
        }

        try
        {
            // Convert the guild to BSON for MongoDB manipulation.
            BsonDocument tree = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
            BsonElement valueElement;

            // Get the value of the specified field, if it exists.
            BsonValue? value = tree.TryGetElement(field, out valueElement) ? valueElement.Value : null;

            // Create a filter to locate the guild in the database.
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Id", guild.Id);

            BsonDocument element = await _collection.Find(filter).FirstOrDefaultAsync();

            if (element != null)
            {
                // Set or remove the field in the database document.
                var update = value != null ? Builders<BsonDocument>.Update.Set(field, value) : Builders<BsonDocument>.Update.Unset(field);
                await _collection.UpdateOneAsync(filter, update);

                return true;
            }
        }
        catch (Exception e)
        {
            string? src = e.Source;

            AresLogger.Error(string.IsNullOrEmpty(src) ? "Exception" : src, "Unable to save data.", e.Message);
        }

        return false;
    }

    /// <summary>
    /// Removes a guild from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the guild to be removed from the cache.</param>
    public void DeleteCache(string id)
    {
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
    /// Retrieves all guilds from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved guilds.</returns>
    public async Task<ConcurrentBag<Model.Guild>> GetGuilds(int limit = 0)
    {
        var accounts = new ConcurrentBag<Model.Guild>();

        if (_collection == null)
        {
            AresLogger.Error("CollectionNull", "Collection returned null when get all guilds.");
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
}