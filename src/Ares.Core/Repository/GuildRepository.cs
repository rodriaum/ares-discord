/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Constants;
using Ares.Core.Models.Collection;
using Ares.Core.Objects;
using Ares.Core.Service;
using Ares.Core.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ares.Core.Repository;

/// <summary>
/// Class responsible for managing guild data in MongoDB database.
/// </summary>
public class GuildRepository
{
    /// <summary>
    /// Represents the "guilds" collection in MongoDB database.
    /// </summary>
    private readonly IMongoCollection<BsonDocument>? _collection;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisService _redisDatabase;

    /// <summary>
    /// Key prefix used for guild data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AresConstant.AppName.ToLower()}:guild:";

    /// <summary>
    /// Dictionary of locks for concurrent operations on the same guild
    /// </summary>
    private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLocks = new ConcurrentDictionary<ulong, SemaphoreSlim>();

    /*
     * Constructors and initialization methods.
     */

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildRepository"/> class with the guilds collection and guild manager.
    /// </summary>
    /// <param name="mongoDatabase">MongoDB database instance that contains the "guilds" collection.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public GuildRepository(MongoService mongoDatabase, RedisService redisDatabase)
    {
        _collection = mongoDatabase.mongoDatabase?.GetCollection<BsonDocument>("guilds");
        _redisDatabase = redisDatabase;

        // Create indexes in the collection to optimize queries.
        CreateIndexesAsync();
    }

    /// <summary>
    /// Creates indexes in the "guilds" collection to improve query performance.
    /// </summary>
    public async void CreateIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: Guild", "Creating indexes in the database...");

        // Check if the collection was initialized before trying to create indexes.
        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when creating guild data indexes.", severity: Severity.Error);
            return;
        }

        try
        {
            IndexKeysDefinition<BsonDocument> indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("id");
            CreateIndexModel<BsonDocument> indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

            await _collection.Indexes.CreateOneAsync(indexModel);

            await AresLogger.LogAsync("Repo: Guild", "Indexes created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("IndexCreationError", $"Error creating indexes: {ex.Message}", severity: Severity.Error);
        }
    }

    /*
     * Database operations.
     */

    /// <summary>
    /// Saves or updates a guild in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="User"/> object representing the saved or updated guild.</returns>
    public async Task<Guild?> SaveAsync(ulong id)
    {
        var semaphore = _guildLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_collection == null)
            {
                await AresLogger.LogAsync("CollectionNull", "Collection returned null when save guild data.", severity: Severity.Error);
                return null;
            }

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

            IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
            BsonDocument element = await cursor.FirstOrDefaultAsync();

            Guild? guild = new Guild(id);

            if (element != null)
            {
                guild = await JsonUtil.BsonDocToObjectAsync<Guild>(element) ?? guild;
            }
            else
            {
                BsonDocument? document = await JsonUtil.ObjectToBsonDocumentAsync(guild);

                if (document != null)
                {
                    // Insert the document in the database if it doesn't exist.
                    await _collection.InsertOneAsync(document);
                }

                await _redisDatabase.SaveAsync(GRedisKey + id, guild);
            }

            return guild;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves a guild from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <returns>A <see cref="User"/> object representing the retrieved guild, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(ulong, bool)"/>
    public async Task<Guild?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        var semaphore = _guildLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            Guild? guild = await _redisDatabase.LoadAsync<Guild>(GRedisKey + id);

            if (guild == null)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

                IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
                BsonDocument element = await cursor.FirstOrDefaultAsync();

                if (element != null)
                {
                    guild = await JsonUtil.BsonDocToObjectAsync<Guild>(element);

                    if (saveInRedis && guild != null)
                        await _redisDatabase.SaveAsync(GRedisKey + id, guild);
                }
            }

            return guild;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Updates a specific field of a guild in the database.
    /// </summary>
    /// <param name="guild">A <see cref="User"/> object representing the guild to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(Guild guild, string field)
    {
        var semaphore = _guildLocks.GetOrAdd(guild.Id, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

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

                await AresLogger.LogAsync("Repo: Guild", $"Updated \"{field}\" for guild \"{guild.Id}\".");
                return true;
            }
            catch (Exception e)
            {
                await AresLogger.LogAsync(e.Source ?? "Exception", "Unable to update guild data.", e.Message, severity: Severity.Error);
                return false;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Removes a guild from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the guild to be removed from the cache.</param>
    public async Task DeleteCache(string id)
    {
        await _redisDatabase.CacheAsync(GRedisKey + id, 300);
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
        ConcurrentBag<Guild> users = new ConcurrentBag<Guild>();

        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when get all guilds.", severity: Severity.Error);
            return users;
        }

        FindOptions<BsonDocument> options = new FindOptions<BsonDocument> { Limit = limit };
        IAsyncCursor<BsonDocument> documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                Guild? guild = await JsonUtil.BsonDocToObjectAsync<Guild>(document);

                if (guild != null)
                    users.Add(guild);
            }
            catch (JsonException ex)
            {
                await AresLogger.LogAsync("JsonReaderException", "Error deserializing document.", ex.Message, severity: Severity.Error);
            }
        });

        return users;
    }

    /// <summary>
    /// Cleanup method to remove unused locks and free memory
    /// </summary>
    public void CleanupLocks(TimeSpan olderThan)
    {
        foreach (var key in _guildLocks.Keys)
        {
            if (_guildLocks.TryRemove(key, out var semaphore))
            {
                semaphore.Dispose();
            }
        }
    }
}