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
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.Json;

namespace Ares.Common.Repository;

/// <summary>
/// Class responsible for managing guild data in PostgreSQL database.
/// </summary>
public class GuildRepository
{
    /// <summary>
    /// Reference to the PostgreSQL database connection.
    /// </summary>
    private readonly PostgresDatabase _database;

    /// <summary>
    /// Reference to the Redis database used for caching operations and related logic.
    /// </summary>
    private readonly RedisDatabase _redisDatabase;

    /// <summary>
    /// Key prefix used for guild data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AppConstants.AppName.ToLower()}:guild:";

    /// <summary>
    /// Table name for guilds in PostgreSQL.
    /// </summary>
    private const string GuildsTable = "guilds";

    /*
     * Constructors and initialization methods.
     */

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildRepository"/> class with the PostgreSQL database.
    /// </summary>
    /// <param name="postgresDatabase">PostgreSQL database instance.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public GuildRepository(PostgresDatabase postgresDatabase, RedisDatabase redisDatabase)
    {
        _database = postgresDatabase;
        _redisDatabase = redisDatabase;
    }

    /// <summary>
    /// Creates the guilds table and indexes to improve query performance.
    /// </summary>
    public async Task CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: Guild", "Checking if table exists in the database...");

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when creating guild table.", severity: Severity.Error);
            return;
        }

        try
        {
            lock (AppCommon.DatabaseLockObject)
            {
                string checkTableSql = $@"SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_schema = 'public' AND table_name = '{GuildsTable}'
                );";

                bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

                if (exists)
                {
                    AresLogger.Log("Repo: Guild", $"Table '{GuildsTable}' already exists in the database.");
                }
                else
                {
                    AresLogger.Log("Repo: Guild", $"Table '{GuildsTable}' not found, creating...");

                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{GuildsTable}"" (");
                    sb.AppendLine(@"""id"" BIGINT NOT NULL,");
                    sb.AppendLine(@"""data"" JSONB NOT NULL,");
                    sb.AppendLine(@"""created_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
                    sb.AppendLine(@"""updated_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
                    sb.AppendLine(@"PRIMARY KEY (""id"")");
                    sb.AppendLine(@");");
                    sb.AppendLine($@"COMMENT ON COLUMN ""{GuildsTable}"".""id"" IS '';");
                    sb.AppendLine($@"COMMENT ON COLUMN ""{GuildsTable}"".""data"" IS '';");
                    sb.AppendLine($@"COMMENT ON COLUMN ""{GuildsTable}"".""created_at"" IS '';");
                    sb.AppendLine($@"COMMENT ON COLUMN ""{GuildsTable}"".""updated_at"" IS '';");

                    _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();

                    AresLogger.Log("Repo: Guild", $"Table '{GuildsTable}' created successfully.");
                }

                string[] indexSqls = {
                    $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{GuildsTable}_id ON {GuildsTable} (id)",
                    $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{GuildsTable}_data_gin ON {GuildsTable} USING GIN (data)"
                };

                foreach (string indexSql in indexSqls)
                {
                    try
                    {
                        _database.ExecuteNonQueryAsync(indexSql).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        AresLogger.Log("IndexCreation", $"Index creation info: {ex.Message}");
                    }
                }
            }

            await AresLogger.LogAsync("Repo: Guild", "Table and indexes checked/created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("TableCreationError", $"Error creating table and indexes: {ex.Message}", severity: Severity.Error);
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
    public async Task<Guild?> SaveAsync(ulong id)
    {
        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when saving guild data.", severity: Severity.Error);
            return null;
        }

        string selectSql = $"SELECT data FROM {GuildsTable} WHERE id = @id";
        var selectParam = new NpgsqlParameter("@id", (long)id);

        try
        {
            string? guildData = await _database.ExecuteScalarAsync<string>(selectSql, selectParam);
            Guild? guild = new Guild(id);

            if (!string.IsNullOrEmpty(guildData))
            {
                guild = JsonSerializer.Deserialize<Guild>(guildData) ?? guild;

                // Alert: Always set the id in case of security, if not set when deserialize
                guild.Id = id;
            }
            else
            {
                // Guild doesn't exist, insert new guild
                string guildJson = JsonSerializer.Serialize(guild);
                string insertSql = $@"
                    INSERT INTO {GuildsTable} (id, data) 
                    VALUES (@id, @data::jsonb)";

                var insertParams = new NpgsqlParameter[]
                {
                    new("@id", (long)id),
                    new("@data", guildJson)
                };

                await _database.ExecuteNonQueryAsync(insertSql, insertParams);
                await _redisDatabase.SaveAsync(GRedisKey + id, guild);
            }

            return guild;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("SaveGuildError", $"Error saving guild {id}: {ex.Message}", severity: Severity.Error);
            return null;
        }
    }

    /// <summary>
    /// Retrieves a guild from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the guild.</param>
    /// <param name="saveInRedis">Whether to save the data in Redis if fetched from database.</param>
    /// <returns>A <see cref="Guild"/> object representing the retrieved guild, or null if not found.</returns>
    public async Task<Guild?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        // Try to get from Redis cache first
        Guild? guild = await _redisDatabase.LoadAsync<Guild>(GRedisKey + id);

        if (guild == null)
        {
            // Not in cache, fetch from database
            if (!_database.IsConnected())
            {
                await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when fetching guild data.", severity: Severity.Error);
                return null;
            }

            try
            {
                string selectSql = $"SELECT data FROM {GuildsTable} WHERE id = @id";
                var param = new NpgsqlParameter("@id", (long)id);

                string? guildData = await _database.ExecuteScalarAsync<string>(selectSql, param);

                if (!string.IsNullOrEmpty(guildData))
                {
                    guild = JsonSerializer.Deserialize<Guild>(guildData);

                    if (guild != null)
                    {
                        // Alert: Always set the id in case of security, if not set when deserialize
                        guild.Id = id;

                        if (saveInRedis)
                        {
                            await _redisDatabase.SaveAsync(GRedisKey + id, guild);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await AresLogger.LogAsync("FetchGuildError", $"Error fetching guild {id}: {ex.Message}", severity: Severity.Error);
            }
        }

        return guild;
    }

    /// <summary>
    /// Updates a specific field of a guild in the database.
    /// </summary>
    /// <param name="guild">A <see cref="Guild"/> object representing the guild to be updated.</param>
    /// <param name="field">Name of the field to be updated (used for logging purposes).</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(Guild guild, string field)
    {
        if (!_database.IsConnected()) return false;

        try
        {
            string guildJson = JsonSerializer.Serialize(guild);
            string updateSql = $@"
                UPDATE {GuildsTable} 
                SET data = @data::jsonb, updated_at = CURRENT_TIMESTAMP 
                WHERE id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", (long)guild.Id),
                new("@data", guildJson)
            };

            int rowsAffected = await _database.ExecuteNonQueryAsync(updateSql, parameters);

            if (rowsAffected > 0)
            {
                // Update Redis
                await _redisDatabase.UpdateAsync(GRedisKey + guild.Id, guild);
                await AresLogger.LogAsync("Repo: Guild", $"Updated \"{field}\" for guild \"{guild.Id}\".");
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            await AresLogger.LogAsync(e.Source ?? "Exception", "Unable to update guild data.", severity: Severity.Error, extra: e.Message);
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
        ConcurrentBag<Guild> guilds = new ConcurrentBag<Guild>();

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when getting all guilds.", severity: Severity.Error);
            return guilds;
        }

        try
        {
            string selectSql = limit > 0
                ? $"SELECT data FROM {GuildsTable} LIMIT @limit"
                : $"SELECT data FROM {GuildsTable}";

            using var reader = limit > 0
                ? await _database.ExecuteReaderAsync(selectSql, new NpgsqlParameter("@limit", limit))
                : await _database.ExecuteReaderAsync(selectSql);

            while (await reader.ReadAsync())
            {
                try
                {
                    string guildData = reader.GetString("data");
                    Guild? guild = JsonSerializer.Deserialize<Guild>(guildData);

                    if (guild != null)
                        guilds.Add(guild);
                }
                catch (JsonException ex)
                {
                    await AresLogger.LogAsync("JsonDeserializationError", "Error deserializing guild data.", severity: Severity.Error, extra: ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("GetAllGuildsError", $"Error retrieving all guilds: {ex.Message}", severity: Severity.Error);
        }

        return guilds;
    }

    /// <summary>
    /// Deletes a guild from the database permanently.
    /// </summary>
    /// <param name="id">Unique ID of the guild to be deleted.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(ulong id)
    {
        if (!_database.IsConnected()) return false;

        try
        {
            string deleteSql = $"DELETE FROM {GuildsTable} WHERE id = @id";
            var param = new NpgsqlParameter("@id", (long)id);

            int rowsAffected = await _database.ExecuteNonQueryAsync(deleteSql, param);

            if (rowsAffected > 0)
            {
                // Also remove from Redis cache
                await DeleteCache(id);
                await AresLogger.LogAsync("Repo: Guild", $"Deleted guild \"{id}\" from database");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("DeleteGuildError", $"Error deleting guild {id}: {ex.Message}", severity: Severity.Error);
            return false;
        }
    }

    /// <summary>
    /// Retrieves guilds by a specific JSON field value.
    /// </summary>
    /// <param name="fieldPath">JSON path to the field (e.g., "settings.prefix").</param>
    /// <param name="value">Value to search for.</param>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved guilds.</returns>
    public async Task<ConcurrentBag<Guild>> GetByFieldAsync(string fieldPath, object value, int limit = 0)
    {
        ConcurrentBag<Guild> guilds = new ConcurrentBag<Guild>();

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when getting guilds by field.", severity: Severity.Error);
            return guilds;
        }

        try
        {
            string selectSql = limit > 0
                ? $"SELECT data FROM {GuildsTable} WHERE data->>@fieldPath = @value LIMIT @limit"
                : $"SELECT data FROM {GuildsTable} WHERE data->>@fieldPath = @value";

            var parameters = limit > 0
                ? new NpgsqlParameter[]
                {
                    new("@fieldPath", fieldPath),
                    new("@value", value.ToString()),
                    new("@limit", limit)
                }
                : new NpgsqlParameter[]
                {
                    new("@fieldPath", fieldPath),
                    new("@value", value.ToString())
                };

            using var reader = await _database.ExecuteReaderAsync(selectSql, parameters);

            while (await reader.ReadAsync())
            {
                try
                {
                    string guildData = reader.GetString("data");
                    Guild? guild = JsonSerializer.Deserialize<Guild>(guildData);

                    if (guild != null)
                        guilds.Add(guild);
                }
                catch (JsonException ex)
                {
                    await AresLogger.LogAsync("JsonDeserializationError", "Error deserializing guild data.", severity: Severity.Error, extra: ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("GetGuildsByFieldError", $"Error retrieving guilds by field {fieldPath}: {ex.Message}", severity: Severity.Error);
        }

        return guilds;
    }
}