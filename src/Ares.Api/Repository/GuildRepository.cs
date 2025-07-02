/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Database.Postgres;
using Ares.Common.Database.Redis;
using Ares.Common.Models.Data;
using Ares.Common.Models.Preference;
using Ares.Common.Objects;
using Ares.Common.Util;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;
using System.Text;

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
    private const string GuildPreferencesTable = "guild_preferences";
    private const string GuildTokensTable = "guild_tokens";

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
                CreateGuildsTable();
                CreateGuildPreferencesTable();
                CreateGuildTokensTable();
            }

            await AresLogger.LogAsync("Repo: Guild", "Table and indexes checked/created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("TableCreationError", $"Error creating table and indexes: {ex.Message}", severity: Severity.Error);
        }
    }

    private void CreateGuildsTable()
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
            sb.AppendLine(@"""created_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine(@"""updated_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine(@"PRIMARY KEY (""id"")");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: Guild", $"Table '{GuildsTable}' created successfully.");
        }

        string indexSql = $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{GuildsTable}_id ON {GuildsTable} (id)";
        TryExecuteNonQuery(indexSql);
    }

    private void CreateGuildPreferencesTable()
    {
        string checkTableSql = $@"SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = '{GuildPreferencesTable}'
        );";

        bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

        if (!exists)
        {
            AresLogger.Log("Repo: Guild", $"Table '{GuildPreferencesTable}' not found, creating...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{GuildPreferencesTable}"" (");
            sb.AppendLine(@"""guild_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""lang"" VARCHAR(10) DEFAULT 'en-US',");
            sb.AppendLine(@"""member_role_id"" BIGINT,");
            sb.AppendLine(@"""usage_role_id"" BIGINT,");
            sb.AppendLine(@"""exclusive_role_id"" BIGINT,");
            sb.AppendLine(@"""setup_channel_id"" BIGINT,");
            sb.AppendLine(@"""log_channel_id"" BIGINT,");
            sb.AppendLine(@"""chats_category_id"" BIGINT,");
            sb.AppendLine(@"PRIMARY KEY (""guild_id""),");
            sb.AppendLine(@"FOREIGN KEY (""guild_id"") REFERENCES ""guilds""(""id"") ON DELETE CASCADE");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: Guild", $"Table '{GuildPreferencesTable}' created successfully.");
        }
    }

    private void CreateGuildTokensTable()
    {
        string checkTableSql = $@"SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = '{GuildTokensTable}'
        );";

        bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

        if (!exists)
        {
            AresLogger.Log("Repo: Guild", $"Table '{GuildTokensTable}' not found, creating...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{GuildTokensTable}"" (");
            sb.AppendLine(@"""guild_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""token_key"" VARCHAR(255) NOT NULL,");
            sb.AppendLine(@"""token_value"" TEXT NOT NULL,");
            sb.AppendLine(@"PRIMARY KEY (""guild_id"", ""token_key""),");
            sb.AppendLine(@"FOREIGN KEY (""guild_id"") REFERENCES ""guilds""(""id"") ON DELETE CASCADE");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: Guild", $"Table '{GuildTokensTable}' created successfully.");
        }
    }

    private void TryExecuteNonQuery(string sql)
    {
        try
        {
            _database.ExecuteNonQueryAsync(sql).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            AresLogger.Log("IndexCreation", $"Index creation info: {ex.Message}");
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

        string selectSql = $"SELECT id FROM {GuildsTable} WHERE id = @id";
        var selectParam = new NpgsqlParameter("@id", (long)id);

        try
        {
            var existingId = await _database.ExecuteScalarAsync<long?>(selectSql, selectParam);
            Guild? guild = new Guild(id);

            if (existingId.HasValue)
            {
                guild = await FetchAsync(id);
            }
            else
            {
                // Guild doesn't exist, insert new guild and its default preferences
                string insertGuildSql = $@"INSERT INTO {GuildsTable} (id) VALUES (@id)";
                await _database.ExecuteNonQueryAsync(insertGuildSql, new NpgsqlParameter("@id", (long)id));

                string insertPrefsSql = $@"INSERT INTO {GuildPreferencesTable} (guild_id) VALUES (@guild_id)";
                await _database.ExecuteNonQueryAsync(insertPrefsSql, new NpgsqlParameter("@guild_id", (long)id));

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
        Guild? guild = await _redisDatabase.LoadAsync<Guild>(GRedisKey + id);
        if (guild != null) return guild;

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when fetching guild data.", severity: Severity.Error);
            return null;
        }

        try
        {
            string selectPrefsSql = $"SELECT * FROM {GuildPreferencesTable} WHERE guild_id = @id";
            using var reader = await _database.ExecuteReaderAsync(selectPrefsSql, new NpgsqlParameter("@id", (long)id));

            if (await reader.ReadAsync())
            {
                guild = new Guild(id)
                {
                    Preferences = new GPreference
                    {
                        Lang = reader.GetString(reader.GetOrdinal("lang")),
                        MemberRoleId = (ulong)reader.GetInt64(reader.GetOrdinal("member_role_id")),
                        UsageRoleId = (ulong)reader.GetInt64(reader.GetOrdinal("usage_role_id")),
                        ExclusiveRoleId = (ulong)reader.GetInt64(reader.GetOrdinal("exclusive_role_id")),
                        SetupChannelId = (ulong)reader.GetInt64(reader.GetOrdinal("setup_channel_id")),
                        LogChannelId = (ulong)reader.GetInt64(reader.GetOrdinal("log_channel_id")),
                        ChatsCategoryId = (ulong)reader.GetInt64(reader.GetOrdinal("chats_category_id"))
                    }
                };
            }
            else
            {
                // If no preferences found, it might be a new guild, return a default object
                return new Guild(id);
            }
            await reader.CloseAsync();

            // Fetch tokens
            string selectTokensSql = $"SELECT token_key, token_value FROM {GuildTokensTable} WHERE guild_id = @id";
            using var tokenReader = await _database.ExecuteReaderAsync(selectTokensSql, new NpgsqlParameter("@id", (long)id));
            while (await tokenReader.ReadAsync())
            {
                guild.Token.List[tokenReader.GetString(0)] = tokenReader.GetString(1);
            }

            if (saveInRedis)
            {
                await _redisDatabase.SaveAsync(GRedisKey + id, guild);
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("FetchGuildError", $"Error fetching guild {id}: {ex.Message}", severity: Severity.Error);
            return null;
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
            // Update Preferences
            var prefs = guild.Preferences;
            string updatePrefsSql = $@"
                UPDATE {GuildPreferencesTable} SET
                    lang = @lang,
                    member_role_id = @member_role_id,
                    usage_role_id = @usage_role_id,
                    exclusive_role_id = @exclusive_role_id,
                    setup_channel_id = @setup_channel_id,
                    log_channel_id = @log_channel_id,
                    chats_category_id = @chats_category_id
                WHERE guild_id = @guild_id";

            await _database.ExecuteNonQueryAsync(updatePrefsSql,
                new NpgsqlParameter("@lang", prefs.Lang),
                new NpgsqlParameter("@member_role_id", (long)prefs.MemberRoleId),
                new NpgsqlParameter("@usage_role_id", (long)prefs.UsageRoleId),
                new NpgsqlParameter("@exclusive_role_id", (long)prefs.ExclusiveRoleId),
                new NpgsqlParameter("@setup_channel_id", (long)prefs.SetupChannelId),
                new NpgsqlParameter("@log_channel_id", (long)prefs.LogChannelId),
                new NpgsqlParameter("@chats_category_id", (long)prefs.ChatsCategoryId),
                new NpgsqlParameter("@guild_id", (long)guild.Id));

            // Update Tokens (delete and re-insert)
            string deleteTokensSql = $"DELETE FROM {GuildTokensTable} WHERE guild_id = @guild_id";
            await _database.ExecuteNonQueryAsync(deleteTokensSql, new NpgsqlParameter("@guild_id", (long)guild.Id));

            foreach (var tokenPair in guild.Token.List)
            {
                string insertTokenSql = $"INSERT INTO {GuildTokensTable} (guild_id, token_key, token_value) VALUES (@guild_id, @key, @value)";
                await _database.ExecuteNonQueryAsync(insertTokenSql,
                    new NpgsqlParameter("@guild_id", (long)guild.Id),
                    new NpgsqlParameter("@key", tokenPair.Key),
                    new NpgsqlParameter("@value", tokenPair.Value));
            }

            // Update timestamp
            string updateTimestampSql = $"UPDATE {GuildsTable} SET updated_at = CURRENT_TIMESTAMP WHERE id = @id";
            await _database.ExecuteNonQueryAsync(updateTimestampSql, new NpgsqlParameter("@id", (long)guild.Id));

            await _redisDatabase.UpdateAsync(GRedisKey + guild.Id, guild);
            await AresLogger.LogAsync("Repo: Guild", $"Updated \"{field}\" for guild \"{guild.Id}\".");
            return true;
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
                ? $"SELECT id FROM {GuildsTable} LIMIT @limit"
                : $"SELECT id FROM {GuildsTable}";

            using var reader = limit > 0
                ? await _database.ExecuteReaderAsync(selectSql, new NpgsqlParameter("@limit", limit))
                : await _database.ExecuteReaderAsync(selectSql);

            while (await reader.ReadAsync())
            {
                var guildId = reader.GetInt64("id");
                var guild = await FetchAsync((ulong)guildId);
                if (guild != null)
                {
                    guilds.Add(guild);
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
    /// Retrieves guilds by a specific preference field value.
    /// </summary>
    /// <param name="fieldName">The name of the column in the guild_preferences table.</param>
    /// <param name="value">Value to search for.</param>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved guilds.</returns>
    public async Task<ConcurrentBag<Guild>> GetByFieldAsync(string fieldName, object value, int limit = 0)
    {
        ConcurrentBag<Guild> guilds = new ConcurrentBag<Guild>();
        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when getting guilds by field.", severity: Severity.Error);
            return guilds;
        }

        try
        {
            // Sanitize fieldName to prevent SQL injection
            var allowedFields = new[] { "lang", "member_role_id", "usage_role_id", "exclusive_role_id", "setup_channel_id", "log_channel_id", "chats_category_id" };
            if (!allowedFields.Contains(fieldName))
            {
                throw new ArgumentException($"Invalid field name: {fieldName}", nameof(fieldName));
            }

            string limitClause = limit > 0 ? "LIMIT @limit" : "";
            string selectSql = $@"SELECT guild_id FROM {GuildPreferencesTable} WHERE ""{fieldName}"" = @value {limitClause}";

            var parameters = new List<NpgsqlParameter> { new NpgsqlParameter("@value", value) };
            if (limit > 0)
            {
                parameters.Add(new NpgsqlParameter("@limit", limit));
            }

            using var reader = await _database.ExecuteReaderAsync(selectSql, parameters.ToArray());
            while (await reader.ReadAsync())
            {
                var guildId = (ulong)reader.GetInt64(0);
                var guild = await FetchAsync(guildId, saveInRedis: true);
                if (guild != null)
                {
                    guilds.Add(guild);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("GetGuildsByFieldError", $"Error retrieving guilds by field {fieldName}: {ex.Message}", severity: Severity.Error);
        }

        return guilds;
    }
}