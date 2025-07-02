/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Database.Postgres;
using Ares.Common.Database.Redis;
using Ares.Common.Models.Chat.Price;
using Ares.Common.Models.Data;
using Ares.Common.Models.Data.Chat.Model;
using Ares.Common.Objects;
using Ares.Common.Objects.Image;
using Ares.Common.Util;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
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
    private const string ModelsTable = "chat_models";
    private const string ModelPricesTable = "chat_model_prices";
    private const string ModelPriceDetailsTable = "chat_model_price_details";

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
    }

    /// <summary>
    /// Creates the chat_models table and indexes to improve query performance.
    /// </summary>
    public async Task CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: Chat Models", "Checking if table exists in the database...");

        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when creating chat models table.", severity: Severity.Error);
            return;
        }

        try
        {
            lock (AppCommon.DatabaseLockObject)
            {
                CreateModelsTable();
                CreateModelPricesTable();
                CreateModelPriceDetailsTable();
            }

            await AresLogger.LogAsync("Repo: Chat Models", "Table and indexes checked/created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("TableCreationError", $"Error creating table and indexes: {ex.Message}", severity: Severity.Error);
        }
    }

    private void CreateModelsTable()
    {
        string checkTableSql = $@"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{ModelsTable}');";
        if (_postgresDatabase.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult())
        {
            AresLogger.Log("Repo: Chat Models", $"Table '{ModelsTable}' already exists.");
            return;
        }

        AresLogger.Log("Repo: Chat Models", $"Table '{ModelsTable}' not found, creating...");
        var sb = new StringBuilder();
        sb.AppendLine($@"CREATE TABLE ""{ModelsTable}"" (");
        sb.AppendLine(@"""id"" VARCHAR(255) PRIMARY KEY,");
        sb.AppendLine(@"""display_name"" VARCHAR(255) NOT NULL,");
        sb.AppendLine(@"""description_key"" TEXT,");
        sb.AppendLine(@"""request_type"" VARCHAR(50) NOT NULL,");
        sb.AppendLine(@"""category"" VARCHAR(50) NOT NULL,");
        sb.AppendLine(@"""type"" VARCHAR(50) NOT NULL,");
        sb.AppendLine(@"""exclusive"" BOOLEAN NOT NULL DEFAULT FALSE,");
        sb.AppendLine(@"""available"" BOOLEAN NOT NULL DEFAULT TRUE,");
        sb.AppendLine(@"""dev"" BOOLEAN NOT NULL DEFAULT FALSE,");
        sb.AppendLine(@"""created_at"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,");
        sb.AppendLine(@"""updated_at"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP);");
        _postgresDatabase.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();

        string[] indexSqls = {
            $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{ModelsTable}_category ON {ModelsTable} (category)",
            $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{ModelsTable}_type ON {ModelsTable} (type)"
        };
        foreach (var sql in indexSqls) TryExecuteNonQuery(sql);
    }

    private void CreateModelPricesTable()
    {
        string checkTableSql = $@"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{ModelPricesTable}');";
        if (_postgresDatabase.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult())
        {
            AresLogger.Log("Repo: Chat Models", $"Table '{ModelPricesTable}' already exists.");
            return;
        }

        AresLogger.Log("Repo: Chat Models", $"Table '{ModelPricesTable}' not found, creating...");
        var sb = new StringBuilder();
        sb.AppendLine($@"CREATE TABLE ""{ModelPricesTable}"" (");
        sb.AppendLine(@"""model_id"" VARCHAR(255) PRIMARY KEY,");
        sb.AppendLine(@"""output_price_token"" DECIMAL(18, 10) NOT NULL,");
        sb.AppendLine(@"""input_price_token"" DECIMAL(18, 10) NOT NULL,");
        sb.AppendLine(@"""input_price_per_image"" DECIMAL(18, 10) NOT NULL,");
        sb.AppendLine($@"FOREIGN KEY (""model_id"") REFERENCES ""{ModelsTable}""(""id"") ON DELETE CASCADE);");
        _postgresDatabase.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
    }

    private void CreateModelPriceDetailsTable()
    {
        string checkTableSql = $@"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{ModelPriceDetailsTable}');";
        if (_postgresDatabase.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult())
        {
            AresLogger.Log("Repo: Chat Models", $"Table '{ModelPriceDetailsTable}' already exists.");
            return;
        }

        AresLogger.Log("Repo: Chat Models", $"Table '{ModelPriceDetailsTable}' not found, creating...");
        var sb = new StringBuilder();
        sb.AppendLine($@"CREATE TABLE ""{ModelPriceDetailsTable}"" (");
        sb.AppendLine(@"""id"" SERIAL PRIMARY KEY,");
        sb.AppendLine(@"""model_id"" VARCHAR(255) NOT NULL,");
        sb.AppendLine(@"""size"" INT NOT NULL,");
        sb.AppendLine(@"""price"" DECIMAL(18, 10) NOT NULL,");
        sb.AppendLine(@"""quality"" INT NOT NULL,");
        sb.AppendLine($@"FOREIGN KEY (""model_id"") REFERENCES ""{ModelsTable}""(""id"") ON DELETE CASCADE);");
        _postgresDatabase.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
    }

    private void TryExecuteNonQuery(string sql)
    {
        try
        {
            _postgresDatabase.ExecuteNonQueryAsync(sql).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            AresLogger.Log("IndexCreation", $"Index creation info: {ex.Message}");
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

        var modelToSave = newModel ?? await FetchAsync(id) ?? new ChatModel(id);

        try
        {
            string upsertModelSql = $@"
                INSERT INTO {ModelsTable} (id, display_name, description_key, request_type, category, type, exclusive, available, dev)
                VALUES (@id, @display_name, @description_key, @request_type, @category, @type, @exclusive, @available, @dev)
                ON CONFLICT (id) DO UPDATE SET
                    display_name = EXCLUDED.display_name,
                    description_key = EXCLUDED.description_key,
                    request_type = EXCLUDED.request_type,
                    category = EXCLUDED.category,
                    type = EXCLUDED.type,
                    exclusive = EXCLUDED.exclusive,
                    available = EXCLUDED.available,
                    dev = EXCLUDED.dev,
                    updated_at = CURRENT_TIMESTAMP;";

            await _postgresDatabase.ExecuteNonQueryAsync(upsertModelSql,
                new NpgsqlParameter("@id", modelToSave.Id),
                new NpgsqlParameter("@display_name", modelToSave.DisplayName),
                new NpgsqlParameter("@description_key", modelToSave.DescriptionKey),
                new NpgsqlParameter("@request_type", modelToSave.RequestType.ToString()),
                new NpgsqlParameter("@category", modelToSave.Category.ToString()),
                new NpgsqlParameter("@type", modelToSave.Type.ToString()),
                new NpgsqlParameter("@exclusive", modelToSave.Exclusive),
                new NpgsqlParameter("@available", modelToSave.Available),
                new NpgsqlParameter("@dev", modelToSave.Dev));

            if (modelToSave.Price != null)
            {
                string upsertPriceSql = $@"
                    INSERT INTO {ModelPricesTable} (model_id, output_price_token, input_price_token, input_price_per_image)
                    VALUES (@model_id, @output_price, @input_price, @image_price)
                    ON CONFLICT (model_id) DO UPDATE SET
                        output_price_token = EXCLUDED.output_price_token,
                        input_price_token = EXCLUDED.input_price_token,
                        input_price_per_image = EXCLUDED.input_price_per_image;";

                await _postgresDatabase.ExecuteNonQueryAsync(upsertPriceSql,
                    new NpgsqlParameter("@model_id", modelToSave.Id),
                    new NpgsqlParameter("@output_price", modelToSave.Price.OutputPriceToken),
                    new NpgsqlParameter("@input_price", modelToSave.Price.InputPriceToken),
                    new NpgsqlParameter("@image_price", modelToSave.Price.InputPricePerImage));

                // Handle Price Details
                string deleteDetailsSql = $"DELETE FROM {ModelPriceDetailsTable} WHERE model_id = @model_id";
                await _postgresDatabase.ExecuteNonQueryAsync(deleteDetailsSql, new NpgsqlParameter("@model_id", modelToSave.Id));

                if (modelToSave.Price.ChatPriceUsageDetail != null)
                {
                    foreach (var detail in modelToSave.Price.ChatPriceUsageDetail)
                    {
                        string insertDetailSql = $@"
                            INSERT INTO {ModelPriceDetailsTable} (model_id, size, price, quality)
                            VALUES (@model_id, @size, @price, @quality);";
                        await _postgresDatabase.ExecuteNonQueryAsync(insertDetailSql,
                            new NpgsqlParameter("@model_id", modelToSave.Id),
                            new NpgsqlParameter("@size", (int)detail.Size),
                            new NpgsqlParameter("@price", detail.Price),
                            new NpgsqlParameter("@quality", (int)detail.Quality));
                    }
                }
            }

            await _redisDatabase.SaveAsync(GRedisKey + id, modelToSave);
            return modelToSave;
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
        if (model != null) return model;

        try
        {
            string selectSql = $@"
                SELECT m.*, p.output_price_token, p.input_price_token, p.input_price_per_image
                FROM {ModelsTable} m
                LEFT JOIN {ModelPricesTable} p ON m.id = p.model_id
                WHERE m.id = @id";

            using var reader = await _postgresDatabase.ExecuteReaderAsync(selectSql, new NpgsqlParameter("@id", id));

            if (await reader.ReadAsync())
            {
                model = new ChatModel(reader.GetString(reader.GetOrdinal("id")))
                {
                    DisplayName = reader.GetString(reader.GetOrdinal("display_name")),
                    DescriptionKey = reader.IsDBNull(reader.GetOrdinal("description_key")) ? string.Empty : reader.GetString(reader.GetOrdinal("description_key")),
                    RequestType = Enum.Parse<ChatRequestType>(reader.GetString(reader.GetOrdinal("request_type"))),
                    Category = Enum.Parse<ModelCategory>(reader.GetString(reader.GetOrdinal("category"))),
                    Type = Enum.Parse<ModelType>(reader.GetString(reader.GetOrdinal("type"))),
                    Exclusive = reader.GetBoolean(reader.GetOrdinal("exclusive")),
                    Available = reader.GetBoolean(reader.GetOrdinal("available")),
                    Dev = reader.GetBoolean(reader.GetOrdinal("dev"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("output_price_token")))
                {
                    model.Price = new ChatPriceUsage(
                        outputPriceToken: reader.GetDecimal(reader.GetOrdinal("output_price_token")),
                        inputPriceToken: reader.GetDecimal(reader.GetOrdinal("input_price_token")),
                        inputPricePerImage: reader.GetDecimal(reader.GetOrdinal("input_price_per_image"))
                    );

                    // Fetch Price Details
                    string selectDetailsSql = $"SELECT size, price, quality FROM {ModelPriceDetailsTable} WHERE model_id = @model_id";
                    using var detailsReader = await _postgresDatabase.ExecuteReaderAsync(selectDetailsSql, new NpgsqlParameter("@model_id", id));
                    model.Price.ChatPriceUsageDetail = new List<ChatPriceUsageDetail>();
                    while (await detailsReader.ReadAsync())
                    {
                        model.Price.ChatPriceUsageDetail.Add(new ChatPriceUsageDetail(
                            size: (ImageSize)detailsReader.GetInt32(detailsReader.GetOrdinal("size")),
                            price: detailsReader.GetDecimal(detailsReader.GetOrdinal("price")),
                            quality: (ImageQuality)detailsReader.GetInt32(detailsReader.GetOrdinal("quality"))
                        ));
                    }
                }

                if (saveInRedis)
                {
                    await _redisDatabase.SaveAsync(GRedisKey + id, model);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("FetchError", $"Error fetching chat model: {ex.Message}", severity: Severity.Error);
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
        // This method can be simplified by first finding the nearest ID, then calling FetchAsync.
        try
        {
            string selectIdSql = $"SELECT id FROM {ModelsTable} WHERE id LIKE @pattern ORDER BY id LIMIT 1";
            string? actualId = await _postgresDatabase.ExecuteScalarAsync<string>(selectIdSql, new NpgsqlParameter("@pattern", $"{id}%"));

            if (actualId != null)
            {
                return await FetchAsync(actualId, saveInRedis);
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("FetchNearestError", $"Error fetching nearest chat model: {ex.Message}", severity: Severity.Error);
        }

        return null;
    }

    /// <summary>
    /// Updates a specific field of a model in the database.
    /// </summary>
    /// <param name="model">A <see cref="ChatModel"/> object representing the model to be updated.</param>
    /// <param name="field">Name of the field to be updated.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(ChatModel model, string field)
    {
        // With the new structure, it's often easier and safer to save the entire object.
        // This ensures consistency across all related tables.
        var savedModel = await SaveAsync(model.Id, model);
        return savedModel != null;
    }

    /// <summary>
    /// Removes a model from the local cache.
    /// </summary>
    /// <param name="id">Unique ID of the model to be removed from the cache.</param>
    public async Task DeleteCache(string id)
    {
        await _redisDatabase.DeleteAsync(GRedisKey + id);
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
        var models = new ConcurrentBag<ChatModel>();
        if (!_postgresDatabase.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not open when getting all models.", severity: Severity.Error);
            return models;
        }

        try
        {
            string selectSql = limit > 0
                ? $"SELECT id FROM {ModelsTable} ORDER BY id LIMIT @limit"
                : $"SELECT id FROM {ModelsTable} ORDER BY id";

            var parameters = limit > 0 ? new[] { new NpgsqlParameter("@limit", limit) } : Array.Empty<NpgsqlParameter>();
            using var reader = await _postgresDatabase.ExecuteReaderAsync(selectSql, parameters);

            var tasks = new List<Task<ChatModel?>>();
            while (await reader.ReadAsync())
            {
                // Fetch each model fully. This could be optimized with a single large JOIN query if performance is critical.
                tasks.Add(FetchAsync(reader.GetString(0), saveInRedis: true));
            }
            
            var results = await Task.WhenAll(tasks);
            foreach(var model in results)
            {
                if (model != null) models.Add(model);
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
            string deleteSql = $"DELETE FROM {ModelsTable} WHERE id = @id";
            int rowsAffected = await _postgresDatabase.ExecuteNonQueryAsync(deleteSql, new NpgsqlParameter("@id", id));

            if (rowsAffected > 0)
            {
                await DeleteCache(id);
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