/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Database.Postgres;
using Ares.Common.Database.Redis;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Util;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;

namespace Ares.Common.Repository;

/// <summary>
/// Class responsible for managing chat models in PostgreSQL database.
/// </summary>
public class ChatModelRepository
{
    /// <summary>
    /// Reference to the PostgreSQL database connection.
    /// </summary>
    private readonly PostgresDatabase _postgresDatabase;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisDatabase _redisDatabase;

    /// <summary>
    /// Key prefix used for model data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AppConstants.AppName.ToLower()}:model:";

    /// <summary>
    /// Table name for chat models in PostgreSQL.
    /// </summary>
    private const string TableName = "chat_models";

    #region Constructors and initialization methods.

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModelRepository"/> class with the PostgreSQL database.
    /// </summary>
    /// <param name="postgresDatabase">PostgreSQL database instance.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public ChatModelRepository(PostgresDatabase postgresDatabase, RedisDatabase redisDatabase)
    {
        _postgresDatabase = postgresDatabase;
        _redisDatabase = redisDatabase;

        // Create table and indexes to optimize queries.
        CreateTableAndIndexesAsync();
    }

    /// <summary>
    /// Creates the chat_models table and indexes to improve query performance.
    /// </summary>
    public async void CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: Chat Models", "Creating table and indexes in the database...");

        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when creating chat models table.", severity: Severity.Error);
            return;
        }

        try
        {
            string[] indexSqls = {
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{TableName}_id ON {TableName} (id)",
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{TableName}_updated_at ON {TableName} (updated_at)",
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{TableName}_data_gin ON {TableName} USING GIN (data)"
            };

            foreach (string indexSql in indexSqls)
            {
                try
                {
                    await _postgresDatabase.ExecuteNonQueryAsync(indexSql);
                }
                catch (Exception ex)
                {
                    // Index might already exist, log but continue
                    await AresLogger.LogAsync("IndexCreation", $"Index creation info: {ex.Message}");
                }
            }

            await AresLogger.LogAsync("Repo: Chat Models", "Table and indexes created/verified.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("TableCreationError", $"Error creating table and indexes: {ex.Message}", severity: Severity.Error);
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
        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when saving chat model data.", severity: Severity.Error);
            return null;
        }

        ChatModel model;
        string selectSql = $"SELECT data FROM {TableName} WHERE id = @id";

        try
        {
            string? existingData = await _postgresDatabase.ExecuteScalarAsync<string>(selectSql,
                new NpgsqlParameter("@id", id));

            if (existingData != null)
            {
                if (newModel != null)
                {
                    model = newModel;
                    string modelJson = JsonSerializer.Serialize(model);

                    string updateSql = $"UPDATE {TableName} SET data = @data, updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                    await _postgresDatabase.ExecuteNonQueryAsync(updateSql,
                        new NpgsqlParameter("@data", NpgsqlDbType.Jsonb) { Value = modelJson },
                        new NpgsqlParameter("@id", id));
                }
                else
                {
                    model = JsonSerializer.Deserialize<ChatModel>(existingData) ?? new ChatModel(id);

                    // Alert: Always set the id in case of security, if not set when deserialize
                    model.Id = id;
                }
            }
            else
            {
                model = newModel ?? new ChatModel(id);
                string modelJson = JsonSerializer.Serialize(model);

                string insertSql = $"INSERT INTO {TableName} (id, data) VALUES (@id, @data)";
                await _postgresDatabase.ExecuteNonQueryAsync(insertSql,
                    new NpgsqlParameter("@id", id),
                    new NpgsqlParameter("@data", NpgsqlDbType.Jsonb) { Value = modelJson });
            }

            await _redisDatabase.SaveAsync(GRedisKey + id, model);
            return model;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("SaveError", $"Error saving chat model: {ex.Message}", severity: Severity.Error);
            return null;
        }
    }

    /// <summary>
    /// Retrieves a model from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the model.</param>
    /// <param name="saveInRedis">Indicates whether to save the retrieved model in Redis cache.</param>
    /// <returns>A <see cref="ChatModel"/> object representing the retrieved model, or null if not found.</returns>
    public async Task<ChatModel?> FetchAsync(string id, bool saveInRedis = false)
    {
        ChatModel? model = await _redisDatabase.LoadAsync<ChatModel>(GRedisKey + id);

        if (model == null)
        {
            try
            {
                string selectSql = $"SELECT data FROM {TableName} WHERE id = @id";
                string? modelData = await _postgresDatabase.ExecuteScalarAsync<string>(selectSql,
                    new NpgsqlParameter("@id", id));

                if (modelData != null)
                {
                    model = JsonSerializer.Deserialize<ChatModel>(modelData);

                    if (model != null)
                    {
                        // Alert: Always set the id in case of security, if not set when deserialize
                        model.Id = id;

                        if (saveInRedis)
                        {
                            await _redisDatabase.SaveAsync(GRedisKey + id, model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await AresLogger.LogAsync("FetchError", $"Error fetching chat model: {ex.Message}", severity: Severity.Error);
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
            try
            {
                string selectSql = $"SELECT id, data FROM {TableName} WHERE id LIKE @pattern ORDER BY id LIMIT 1";

                using var reader = await _postgresDatabase.ExecuteReaderAsync(selectSql,
                    new NpgsqlParameter("@pattern", $"{id}%"));

                if (await reader.ReadAsync())
                {
                    string actualId = reader.GetString("id");
                    string modelData = reader.GetString("data");

                    model = JsonSerializer.Deserialize<ChatModel>(modelData);

                    if (saveInRedis && model != null)
                        await _redisDatabase.SaveAsync(GRedisKey + actualId, model);
                }
            }
            catch (Exception ex)
            {
                await AresLogger.LogAsync("FetchNearestError", $"Error fetching nearest chat model: {ex.Message}", severity: Severity.Error);
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
        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when updating chat model.", severity: Severity.Error);
            return false;
        }

        try
        {
            // Get the field value using reflection
            var property = typeof(ChatModel).GetProperty(field);
            if (property == null)
            {
                await AresLogger.LogAsync("PropertyNotFound", $"Property '{field}' not found in ChatModel.", severity: Severity.Error);
                return false;
            }

            object? fieldValue = property.GetValue(model);

            // Update the specific field in the JSONB data
            string updateSql;
            NpgsqlParameter[] parameters;

            if (fieldValue != null)
            {
                string fieldValueJson = JsonSerializer.Serialize(fieldValue);
                updateSql = $"UPDATE {TableName} SET data = jsonb_set(data, '{{{field}}}', @value), updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                parameters = new[]
                {
                    new NpgsqlParameter("@value", NpgsqlDbType.Jsonb) { Value = fieldValueJson },
                    new NpgsqlParameter("@id", model.Id)
                };
            }
            else
            {
                updateSql = $"UPDATE {TableName} SET data = data - @field, updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                parameters = new[]
                {
                    new NpgsqlParameter("@field", field),
                    new NpgsqlParameter("@id", model.Id)
                };
            }

            int rowsAffected = await _postgresDatabase.ExecuteNonQueryAsync(updateSql, parameters);

            if (rowsAffected > 0)
            {
                // Update Redis
                await _redisDatabase.UpdateAsync(GRedisKey + model.Id, model);
                await AresLogger.LogAsync("Repo: Chat Models", $"Updated \"{field}\" for model \"{model.Id}\".");
                return true;
            }

            return false;
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

        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when getting all models.", severity: Severity.Error);
            return models;
        }

        try
        {
            string selectSql = limit > 0
                ? $"SELECT data FROM {TableName} ORDER BY created_at LIMIT @limit"
                : $"SELECT data FROM {TableName} ORDER BY created_at";

            NpgsqlParameter[] parameters = limit > 0
                ? new[] { new NpgsqlParameter("@limit", limit) }
                : Array.Empty<NpgsqlParameter>();

            using var reader = await _postgresDatabase.ExecuteReaderAsync(selectSql, parameters);

            while (await reader.ReadAsync())
            {
                try
                {
                    string modelData = reader.GetString("data");
                    ChatModel? model = JsonSerializer.Deserialize<ChatModel>(modelData);

                    if (model != null)
                        models.Add(model);
                }
                catch (JsonException ex)
                {
                    await AresLogger.LogAsync("JsonDeserializationError", "Error deserializing model data.", severity: Severity.Error, extra: ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("GetAllError", $"Error retrieving all models: {ex.Message}", severity: Severity.Error);
        }

        return models;
    }

    /// <summary>
    /// Permanently deletes a model from the database.
    /// </summary>
    /// <param name="id">Unique ID of the model to be deleted.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when deleting chat model.", severity: Severity.Error);
            return false;
        }

        try
        {
            string deleteSql = $"DELETE FROM {TableName} WHERE id = @id";
            int rowsAffected = await _postgresDatabase.ExecuteNonQueryAsync(deleteSql,
                new NpgsqlParameter("@id", id));

            if (rowsAffected > 0)
            {
                // Remove from Redis cache
                await _redisDatabase.DeleteAsync(GRedisKey + id);
                await AresLogger.LogAsync("Repo: Chat Models", $"Deleted model \"{id}\" from database.");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("DeleteError", $"Error deleting chat model: {ex.Message}", severity: Severity.Error);
            return false;
        }
    }

    #endregion
}