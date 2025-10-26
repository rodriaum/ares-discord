/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Api.Database.Mongo;
using Ares.Common.Database.Redis;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ares.Api.Repository;

/// <summary>
/// Class responsible for managing user data in MongoDB database.
/// </summary>
public class UserRepository
{
    /// <summary>
    /// Represents the "users" collection in MongoDB database.
    /// </summary>
    private readonly IMongoCollection<BsonDocument>? _collection;

    /// <summary>
    /// 
    /// </summary>
    private readonly MongoDatabase _mongoDatabase;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisDatabase _redisDatabase;

    /// <summary>
    /// Key prefix used for user data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AppConstants.AppName.ToLower()}:user:";

    /*
     * Constructors and initialization methods.
     */

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class with the users collection.
    /// </summary>
    /// <param name="mongoDatabase">MongoDB database instance that contains the "users" collection.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public UserRepository(MongoDatabase mongoDatabase, RedisDatabase redisDatabase)
    {
        _mongoDatabase = mongoDatabase;
        _redisDatabase = redisDatabase;
        _collection = _mongoDatabase.mongoDatabase?.GetCollection<BsonDocument>("users");
    }

    /// <summary>
    /// Creates indexes in the "users" collection to improve query performance.
    /// </summary>
    public async Task CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: User", "Creating indexes in the database...");

        // Check if the collection was initialized before trying to create indexes.
        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when creating user data indexes.", severity: Severity.Error);
            return;
        }

        try
        {
            IndexKeysDefinition<BsonDocument> indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("id");
            CreateIndexModel<BsonDocument> indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

            await _collection.Indexes.CreateOneAsync(indexModel);

            await AresLogger.LogAsync("Repo: User", "Indexes created.");
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
    /// Saves or updates a user in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the user.</param>
    /// <returns>A <see cref="User"/> object representing the saved or updated user.</returns>
    public async Task<User?> SaveAsync(ulong id)
    {
        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when save user data.", severity: Severity.Error);
            return null;
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

        IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
        BsonDocument element = await cursor.FirstOrDefaultAsync();

        User? user = new User(id);

        if (element != null)
        {
            user = await JsonUtil.BsonDocToObjectAsync<User>(element) ?? user;
        }
        else
        {
            BsonDocument? document = await JsonUtil.ObjectToBsonDocumentAsync(user);

            if (document != null)
            {
                // Insert the document in the database if it doesn't exist.
                await _collection.InsertOneAsync(document);
            }

            await _redisDatabase.SaveAsync(GRedisKey + id, user);
        }

        return user;
    }

    /// <summary>
    /// Retrieves a user from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the user.</param>
    /// <returns>A <see cref="User"/> object representing the retrieved user, or null if not found.</returns>
    /// <returns>A <see cref="bool"/> if you need to save the fetch data in redis</returns>
    /// <seealso cref="FetchAsync(ulong, bool)"/>
    public async Task<User?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        User? user = await _redisDatabase.LoadAsync<User>(GRedisKey + id);

        if (user == null)
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

            IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
            BsonDocument element = await cursor.FirstOrDefaultAsync();

            if (element != null)
            {
                user = await JsonUtil.BsonDocToObjectAsync<User>(element);

                if (saveInRedis && user != null)
                    await _redisDatabase.SaveAsync(GRedisKey + id, user);
            }
        }

        return user;
    }

    /// <summary>
    /// Updates a specific field of a user in the database.
    /// </summary>
    /// <param name="user">A <see cref="User"/> object representing the user to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(User user, string field)
    {
        if (_collection == null) return false;

        try
        {
            BsonDocument? tree = await JsonUtil.ObjectToBsonDocumentAsync(user);
            if (tree == null) return false;

            if (!tree.TryGetValue(field, out BsonValue? value))
                value = null;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", user.Id);

            UpdateDefinition<BsonDocument> update = value != null
                ? Builders<BsonDocument>.Update.Set(field, value)
                : Builders<BsonDocument>.Update.Unset(field);

            // Update MongoDB
            await _collection.UpdateOneAsync(filter, update);

            // Update Redis
            await _redisDatabase.UpdateAsync(GRedisKey + user.Id, user);

            await AresLogger.LogAsync("Repo: User", $"Updated \"{field}\" for user \"{user.Id}\"");
            return true;
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync(e.Source ?? "Exception", "Unable to update user data.", severity: Severity.Error, extra: e.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes a user from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the user to be removed from the cache.</param>
    public async Task DeleteCache(string id)
    {
        await _redisDatabase.CacheAsync(GRedisKey + id, 300);
    }

    /// <summary>
    /// Removes a user from the local cache.
    /// </summary>
    /// <param name="id">Ulong of the user to be removed from the cache.</param>
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
    /// Deletes a user from the database and cache by its ID.
    /// </summary>
    /// <param name="id">Unique ID of the user to delete.</param>
    /// <returns>True if deleted, false otherwise.</returns>
    public async Task<bool> DeleteAsync(ulong id)
    {
        if (_collection == null) return false;
        try
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);
            var result = await _collection.DeleteOneAsync(filter);
            await _redisDatabase.DeleteAsync(GRedisKey + id);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("Repo: User", $"Error deleting user {id}: {ex.Message}", severity: Severity.Error);
            return false;
        }
    }

    /// <summary>
    /// Retrieves all users from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of users to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved users.</returns>
    public async Task<ConcurrentBag<User>> GetAllAsync(int limit = 0)
    {
        ConcurrentBag<User> users = new ConcurrentBag<User>();

        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when get all users.", severity: Severity.Error);
            return users;
        }

        FindOptions<BsonDocument> options = new FindOptions<BsonDocument> { Limit = limit };
        IAsyncCursor<BsonDocument> documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                User? user = await JsonUtil.BsonDocToObjectAsync<User>(document);

                if (user != null)
                    users.Add(user);
            }
            catch (JsonException ex)
            {
                await AresLogger.LogAsync("JsonReaderException", "Error deserializing document.", severity: Severity.Error, extra: ex.Message);
            }
        });

        return users;
    }
}