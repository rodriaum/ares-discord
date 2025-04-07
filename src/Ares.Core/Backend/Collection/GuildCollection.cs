/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Core.Backend.Database.Mongo;
using Ares.Ares.Core.Backend.Database.Redis;
using Ares.Ares.Core.Database.Collection;
using Ares.Ares.Core.Database.Repository;
using Ares.Core.Database.Model;
using Ares.Core.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ares.Core.Database.Collection;

/// <summary>
/// Class responsible for managing guild data in MongoDB database.
/// </summary>
internal class GuildCollection : ICollectionTemplate
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
    private readonly string GRedisKey = "guild:";

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
        this._collection = mongoDatabase.mongoDatabase?.GetCollection<BsonDocument>("guilds");
        this._redisDatabase = redisDatabase;

        this._manager = AresCore.GuildManager;

        // Create indexes in the collection to optimize queries.
        this.CreateIndexesAsync();
    }

    /// <summary>
    /// Creates indexes in the "guilds" collection to improve query performance.
    /// </summary>
    public async void CreateIndexesAsync()
    {
        await AresLogger.LogAsync("DB: Mongo", "Creating indexes in the database...");

        // Check if the collection was initialized before trying to create indexes.
        if (this._collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when creating guild data indexes.");
            return;
        }

        try
        {
            IndexKeysDefinition<BsonDocument> indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Id");
            CreateIndexModel<BsonDocument> indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

            await _collection.Indexes.CreateManyAsync(new List<CreateIndexModel<BsonDocument>> { indexModel }).ConfigureAwait(false);

            await AresLogger.LogAsync("DB: Mongo", "Indexes created.");
        }
        catch (Exception ex)
        {
            await AresLogger.ErrorAsync("IndexCreationError", $"Error creating indexes: {ex.Message}");
        }
    }

    /*
     * Database operations.
     */

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Guild?> SaveAsync(string id)
    {
        if (_collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when save guild data.");
            return null;
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

        IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
        BsonDocument element = await cursor.FirstOrDefaultAsync();

        Guild? guild = new Guild(id);

        if (element != null)
        {
            guild = await JsonUtil.BsonDocumentToObjectAsync<Guild>(element) ?? guild;
        }
        else
        {
            string json = await JsonUtil.ObjectToStringAsync<Guild>(guild);
            BsonDocument document = BsonDocument.Parse(json);

            // Insert the document in the database if it doesn't exist.
            await _collection.InsertOneAsync(document);

            await _redisDatabase.SaveAsync(GRedisKey + id, guild);
            _manager.Save(guild);
        }

        return guild;
    }

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Ulong of the guild.</param>
    /// <returns>A <see cref="Guild"/> object representing the saved or updated guild.</returns>
    public async Task<Guild?> SaveAsync(ulong id)
    {
        return await SaveAsync(id.ToString());
    }

    /// <summary>
    /// Retrieves a guild from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="Guild"/> object representing the retrieved guild, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(ulong, bool)"/>
    public async Task<Guild?> FetchAsync(string id, bool saveInRedis = false)
    {
        Guild? guild = _manager.Fetch(id);

        if (guild == null)
        {
            guild = await _redisDatabase.LoadAsync<Guild>(GRedisKey + id);

            if (guild == null)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

                IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
                BsonDocument element = await cursor.FirstOrDefaultAsync();

                if (element != null)
                {
                    guild = await JsonUtil.BsonDocumentToObjectAsync<Guild>(element);

                    if (saveInRedis && guild != null)
                    {
                        await _redisDatabase.SaveAsync(GRedisKey + id, guild);
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
    /// <returns>A <see cref="Guild"/> object representing the retrieved guild, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(string, bool)"/>
    public async Task<Guild?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        return await FetchAsync(id.ToString(), saveInRedis);
    }

    /// <summary>
    /// Updates a specific field of a guild in the database.
    /// </summary>
    /// <param name="guild">A <see cref="Guild"/> object representing the guild to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(Guild guild, string field)
    {
        if (_collection == null) return false;

        try
        {
            BsonDocument? tree = await JsonUtil.ObjectToBsonDocumentAsync(guild);
            if (tree == null) return false;

            if (!tree.TryGetValue(field, out BsonValue? value))
                value = null;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", guild.Id);

            UpdateDefinition<BsonDocument> update = value != null
                ? Builders<BsonDocument>.Update.Set(field, value)
                : Builders<BsonDocument>.Update.Unset(field);

            // Update MongoDB
            await _collection.UpdateOneAsync(filter, update);

            // Update Redis
            await _redisDatabase.UpdateAsync(GRedisKey + guild.Id, guild);

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
    public async Task DeleteCache(string id)
    {
        await _redisDatabase.CacheAsync(GRedisKey + id, 300);
        // Maybe not much lag. It may change in the future.
        _manager?.Delete(id);
    }

    /// <summary>
    /// Removes a guild from the local cache.
    /// </summary>
    /// <param name="id">Ulong of the guild to be removed from the cache.</param>
    public async Task DeleteCache(ulong id)
    {
        await DeleteCache(id.ToString());
    }

    /// <summary>
    /// Removes the expiration time from the specified key, making it persistent.
    /// </summary>
    /// <param name="id">The unique identifier for the key to be persisted.</param>
    public async Task<bool> PersistAsync(string id)
    {
        return await _redisDatabase.PersistAsync(GRedisKey + id);
    }

    /// <summary>
    /// Retrieves all guilds from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved guilds.</returns>
    public async Task<ConcurrentBag<Guild>> GetAllAsync(int limit = 0)
    {
        ConcurrentBag<Guild> accounts = new ConcurrentBag<Guild>();

        if (_collection == null)
        {
            await AresLogger.ErrorAsync("CollectionNull", "Collection returned null when get all guilds.");
            return accounts;
        }

        FindOptions<BsonDocument> options = new FindOptions<BsonDocument> { Limit = limit };
        IAsyncCursor<BsonDocument> documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                Guild? guild = await JsonUtil.BsonDocumentToObjectAsync<Guild>(document);

                if (guild != null)
                    accounts.Add(guild);
            }
            catch (JsonException ex)
            {
                await AresLogger.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
            }
        });

        return accounts;
    }
}