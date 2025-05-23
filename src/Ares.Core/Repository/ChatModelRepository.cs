/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Database.Mongo;
using Ares.Core.Database.Redis;
using Ares.Core.Models.Chat.Model;
using Ares.Core.Objects;
using Ares.Core.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ares.Core.Repository;

/// <summary>
/// Class responsible for managing chat models in MongoDB database.
/// </summary>
public class ChatModelRepository
{
    /// <summary>
    /// Represents the "chat_models" collection in MongoDB database.
    /// </summary>
    private readonly IMongoCollection<BsonDocument>? _collection;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisDatabase _redisDatabase;

    /// <summary>
    /// Key prefix used for guild data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AppConstants.AppName.ToLower()}:model:";

    #region Constructors and initialization methods.

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModelRepository"/> class with the models collection.
    /// </summary>
    /// <param name="mongoDatabase">MongoDB database instance that contains the "chat_models" collection.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public ChatModelRepository(MongoDatabase mongoDatabase, RedisDatabase redisDatabase)
    {
        _collection = mongoDatabase.mongoDatabase?.GetCollection<BsonDocument>("chat_models");
        _redisDatabase = redisDatabase;

        // Create indexes in the collection to optimize queries.
        CreateIndexesAsync();
    }

    /// <summary>
    /// Creates indexes in the "guilds" collection to improve query performance.
    /// </summary>
    public async void CreateIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: Chat Models", "Creating indexes in the database...");

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

            await AresLogger.LogAsync("Repo: Chat Models", "Indexes created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("IndexCreationError", $"Error creating indexes: {ex.Message}", severity: Severity.Error);
        }
    }

    #endregion

    #region Database operations.

    /// <summary>
    /// Saves or updates a model in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Model ID of the model.</param>
    /// <param name="newModel">Optional new model data to save. If not provided, the existing model will be updated.</param>
    /// <returns>A <see cref="ChatModel"/> object representing the saved or updated model.</returns>
    public async Task<ChatModel?> SaveAsync(string id, ChatModel? newModel = null)
    {
        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when save chat model data.", severity: Severity.Error);
            return null;
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);
        IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
        BsonDocument element = await cursor.FirstOrDefaultAsync();

        ChatModel model;

        if (element != null)
        {
            if (newModel != null)
            {
                model = newModel;

                BsonDocument? document = await JsonUtil.ObjectToBsonDocumentAsync(model);

                if (document != null)
                {
                    await _collection.ReplaceOneAsync(filter, document);
                }
            }
            else
            {
                model = await JsonUtil.BsonDocToObjectAsync<ChatModel>(element) ?? new ChatModel(id);
            }
        }
        else
        {
            model = newModel ?? new ChatModel(id);

            BsonDocument? document = await JsonUtil.ObjectToBsonDocumentAsync(model);

            if (document != null)
            {
                await _collection.InsertOneAsync(document);
            }
        }

        await _redisDatabase.SaveAsync(GRedisKey + id, model);

        return model;
    }

    /// <summary>
    /// Retrieves a model from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the model.</param>
    /// <param name="saveInRedis">Indicates whether to save the retrieved model in Redis cache.</param>
    /// <param name="deleteFromRedis">Indicates whether to delete the model from Redis cache.</param>
    /// <returns>A <see cref="ChatModel"/> object representing the retrieved model, or null if not found.</returns>
    public async Task<ChatModel?> FetchAsync(string id, bool saveInRedis = false)
    {
        ChatModel? model = await _redisDatabase.LoadAsync<ChatModel>(GRedisKey + id);

        if (model == null)
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", id);

            IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter);
            BsonDocument element = await cursor.FirstOrDefaultAsync();

            if (element != null)
            {
                model = await JsonUtil.BsonDocToObjectAsync<ChatModel>(element);

                if (saveInRedis && model != null)
                    await _redisDatabase.SaveAsync(GRedisKey + id, model);
            }
        }

        return model;
    }

    /// <summary>
    /// Retrieves a model from the cache or database using its ID, but get first the nearest model.
    /// </summary>
    /// <param name="id">Unique ID of the model.</param>
    /// <param name="saveInRedis">Indicates whether to save the retrieved model in Redis cache.</param>
    /// <returns>A <see cref="ChatModel"/> object representing the retrieved model, or null if not found.</returns>
    public async Task<ChatModel?> FetchByNearestModelAsync(string id, bool saveInRedis = false)
    {
        ChatModel? model = await _redisDatabase.LoadAsync<ChatModel>(GRedisKey + id);

        if (model == null)
        {
            var regexPattern = "^" + Regex.Escape(id);
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Regex("id", new BsonRegularExpression(regexPattern));

            var options = new FindOptions<BsonDocument> { Sort = Builders<BsonDocument>.Sort.Ascending("id") };

            IAsyncCursor<BsonDocument> cursor = await _collection.FindAsync(filter, options);
            BsonDocument element = await cursor.FirstOrDefaultAsync();

            if (element != null)
            {
                model = await JsonUtil.BsonDocToObjectAsync<ChatModel>(element);
                if (saveInRedis && model != null)
                    await _redisDatabase.SaveAsync(GRedisKey + model.Id, model);
            }
        }

        return model;
    }

    /// <summary>
    /// Updates a specific field of a model in the database.
    /// </summary>
    /// <param name="model">A <see cref="ChatModel"/> object representing the model to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(ChatModel model, string field)
    {
        if (_collection == null) return false;

        try
        {
            BsonDocument? tree = await JsonUtil.ObjectToBsonDocumentAsync(model);
            if (tree == null) return false;

            if (!tree.TryGetValue(field, out BsonValue? value))
                value = null;

            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", model.Id);

            UpdateDefinition<BsonDocument> update = value != null
                ? Builders<BsonDocument>.Update.Set(field, value)
                : Builders<BsonDocument>.Update.Unset(field);

            // Update MongoDB
            await _collection.UpdateOneAsync(filter, update);

            // Update Redis
            await _redisDatabase.UpdateAsync(GRedisKey + model.Id, model);

            await AresLogger.LogAsync("Repo: Chat Models", $"Updated \"{field}\" for model \"{model.Id}\".");
            return true;
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync(e.Source ?? "Exception", "Unable to update model data.", severity: Severity.Error, extra: e.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes a model from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the model to be removed from the cache.</param>
    public async Task DeleteCache(string id)
    {
        await _redisDatabase.CacheAsync(GRedisKey + id, 300);
    }

    /// <summary>
    /// Removes a model from the local cache.
    /// </summary>
    /// <param name="id">Ulong of the model to be removed from the cache.</param>
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
    /// Retrieves all models from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of models to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved models.</returns>
    public async Task<ConcurrentBag<ChatModel>> GetAllAsync(int limit = 0)
    {
        ConcurrentBag<ChatModel> models = new ConcurrentBag<ChatModel>();

        if (_collection == null)
        {
            await AresLogger.LogAsync("CollectionNull", "Collection returned null when get all models.", severity: Severity.Error);
            return models;
        }

        FindOptions<BsonDocument> options = new FindOptions<BsonDocument> { Limit = limit };
        IAsyncCursor<BsonDocument> documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                ChatModel? model = await JsonUtil.BsonDocToObjectAsync<ChatModel>(document);

                if (model != null)
                    models.Add(model);
            }
            catch (JsonException ex)
            {
                await AresLogger.LogAsync("JsonReaderException", "Error deserializing document.", severity: Severity.Error, extra: ex.Message);
            }
        });

        return models;
    }

    #endregion
}