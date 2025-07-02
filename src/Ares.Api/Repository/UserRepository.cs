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
/// Class responsible for managing user data in PostgreSQL database.
/// </summary>
public class UserRepository
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
    /// Key prefix used for user data in Redis.
    /// </summary>
    private readonly string GRedisKey = $"{AppConstants.AppName.ToLower()}:user:";

    /// <summary>
    /// Table name for users in PostgreSQL.
    /// </summary>
    private const string UsersTable = "users";
    private const string UserChatsTable = "user_chats";
    private const string UserChatSnippetsTable = "user_chat_snippets";

    /*
     * Constructors and initialization methods.
     */

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class with the PostgreSQL database.
    /// </summary>
    /// <param name="postgresDatabase">PostgreSQL database instance.</param>
    /// <param name="redisDatabase">Redis database instance used for caching operations.</param>
    public UserRepository(PostgresDatabase postgresDatabase, RedisDatabase redisDatabase)
    {
        _database = postgresDatabase;
        _redisDatabase = redisDatabase;
    }

    /// <summary>
    /// Creates the users table and indexes if they don't exist.
    /// </summary>
    public async Task CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: User", "Checking if table exists in the database...");

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when creating user table.", severity: Severity.Error);
            return;
        }

        try
        {
            lock (AppCommon.DatabaseLockObject)
            {
                // Create Users Table
                CreateUsersTable();

                // Create User Chats Table
                CreateUserChatsTable();

                // Create User Chat Snippets Table
                CreateUserChatSnippetsTable();
            }

            await AresLogger.LogAsync("Repo: User", "Table and indexes checked/created.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("TableCreationError", $"Error creating table and indexes: {ex.Message}", severity: Severity.Error);
        }
    }

    private void CreateUsersTable()
    {
        string checkTableSql = $@"SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = '{UsersTable}'
        );";

        bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

        if (exists)
        {
            AresLogger.Log("Repo: User", $"Table '{UsersTable}' already exists in the database.");
        }
        else
        {
            AresLogger.Log("Repo: User", $"Table '{UsersTable}' not found, creating...");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{UsersTable}"" (");
            sb.AppendLine(@"""id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""created_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine(@"""updated_at"" TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine(@"PRIMARY KEY (""id"")");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: User", $"Table '{UsersTable}' created successfully.");
        }

        string indexSql = $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{UsersTable}_id ON {UsersTable} (id)";
        TryExecuteNonQuery(indexSql);
    }

    private void CreateUserChatsTable()
    {
        string checkTableSql = $@"SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = '{UserChatsTable}'
        );";

        bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

        if (!exists)
        {
            AresLogger.Log("Repo: User", $"Table '{UserChatsTable}' not found, creating...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{UserChatsTable}"" (");
            sb.AppendLine(@"""id"" SERIAL PRIMARY KEY,");
            sb.AppendLine(@"""user_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""channel_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""data"" JSONB NOT NULL,");
            sb.AppendLine(@"FOREIGN KEY (""user_id"") REFERENCES ""users""(""id"") ON DELETE CASCADE");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: User", $"Table '{UserChatsTable}' created successfully.");

            string[] indexSqls = {
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{UserChatsTable}_user_id ON {UserChatsTable} (user_id)",
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{UserChatsTable}_channel_id ON {UserChatsTable} (channel_id)"
            };

            foreach (var indexSql in indexSqls)
            {
                TryExecuteNonQuery(indexSql);
            }
        }
    }

    private void CreateUserChatSnippetsTable()
    {
        string checkTableSql = $@"SELECT EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_name = '{UserChatSnippetsTable}'
        );";

        bool exists = _database.ExecuteScalarAsync<bool>(checkTableSql).GetAwaiter().GetResult();

        if (!exists)
        {
            AresLogger.Log("Repo: User", $"Table '{UserChatSnippetsTable}' not found, creating...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"CREATE TABLE IF NOT EXISTS ""{UserChatSnippetsTable}"" (");
            sb.AppendLine(@"""id"" SERIAL PRIMARY KEY,");
            sb.AppendLine(@"""user_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""channel_id"" BIGINT NOT NULL,");
            sb.AppendLine(@"""data"" JSONB NOT NULL,");
            sb.AppendLine(@"FOREIGN KEY (""user_id"") REFERENCES ""users""(""id"") ON DELETE CASCADE");
            sb.AppendLine(@");");

            _database.ExecuteNonQueryAsync(sb.ToString()).GetAwaiter().GetResult();
            AresLogger.Log("Repo: User", $"Table '{UserChatSnippetsTable}' created successfully.");

            string[] indexSqls = {
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{UserChatSnippetsTable}_user_id ON {UserChatSnippetsTable} (user_id)",
                $"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_{UserChatSnippetsTable}_channel_id ON {UserChatSnippetsTable} (channel_id)"
            };

            foreach (var indexSql in indexSqls)
            {
                TryExecuteNonQuery(indexSql);
            }
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
    /// Saves or updates a user in the database, returning the updated object.
    /// </summary>
    /// <param name="id">Unique ID of the user.</param>
    /// <returns>A <see cref="User"/> object representing the saved or updated user.</returns>
    public async Task<User?> SaveAsync(ulong id)
    {
        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when saving user data.", severity: Severity.Error);
            return null;
        }

        string selectSql = $"SELECT id FROM {UsersTable} WHERE id = @id";
        var selectParam = new NpgsqlParameter("@id", (long)id);

        try
        {
            var existingId = await _database.ExecuteScalarAsync<long?>(selectSql, selectParam);
            User? user = new User(id);

            if (existingId.HasValue)
            {
                // User exists, load their related data
                user = await FetchAsync(id);
            }
            else
            {
                // User doesn't exist, insert new user
                string insertSql = $@"INSERT INTO {UsersTable} (id) VALUES (@id)";
                await _database.ExecuteNonQueryAsync(insertSql, new NpgsqlParameter("@id", (long)id));
                await _redisDatabase.SaveAsync(GRedisKey + id, user);
            }

            return user;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("SaveUserError", $"Error saving user {id}: {ex.Message}", severity: Severity.Error);
            return null;
        }
    }

    /// <summary>
    /// Retrieves a user from the cache or database using its ID.
    /// </summary>
    /// <param name="id">Unique ID of the user.</param>
    /// <param name="saveInRedis">Whether to save the data in Redis if fetched from database.</param>
    /// <returns>A <see cref="User"/> object representing the retrieved user, or null if not found.</returns>
    public async Task<User?> FetchAsync(ulong id, bool saveInRedis = false)
    {
        // Try to get from Redis cache first
        User? user = await _redisDatabase.LoadAsync<User>(GRedisKey + id);

        if (user == null)
        {
            // Not in cache, fetch from database
            if (!_database.IsConnected())
            {
                await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when fetching user data.", severity: Severity.Error);
                return null;
            }

            try
            {
                string selectSql = $"SELECT id FROM {UsersTable} WHERE id = @id";
                var param = new NpgsqlParameter("@id", (long)id);

                var userId = await _database.ExecuteScalarAsync<long?>(selectSql, param);

                if (userId.HasValue)
                {
                    user = new User((ulong)userId.Value);
                    // TODO: Implement fetching from the new relational tables (user_chats, etc.)
                    // For now, we return a new User object. You'll need to query the
                    // user_chats and user_chat_snippets tables and populate the user.Chat property.

                    if (saveInRedis)
                    {
                        await _redisDatabase.SaveAsync(GRedisKey + id, user);
                    }
                }
            }
            catch (Exception ex)
            {
                await AresLogger.LogAsync("FetchUserError", $"Error fetching user {id}: {ex.Message}", severity: Severity.Error);
            }
        }

        return user;
    }

    /// <summary>
    /// Updates a specific field of a user in the database.
    /// </summary>
    /// <param name="user">A <see cref="User"/> object representing the user to be updated.</param>
    /// <param name="field">Name of the field to be updated (used for logging purposes).</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(User user, string field)
    {
        if (!_database.IsConnected()) return false;

        try
        {
            // TODO: Implement logic to update relational tables.
            // For example, if 'field' is 'Chat', you would update the 'user_chats' table.
            // This requires a more complex logic than just updating a JSON blob.
            // For now, we'll just update the 'updated_at' timestamp.

            string updateSql = $@"UPDATE {UsersTable} SET updated_at = CURRENT_TIMESTAMP WHERE id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", (long)user.Id) };

            int rowsAffected = await _database.ExecuteNonQueryAsync(updateSql, parameters);

            if (rowsAffected > 0)
            {
                // Update Redis
                await _redisDatabase.UpdateAsync(GRedisKey + user.Id, user);
                await AresLogger.LogAsync("Repo: User", $"Updated \"{field}\" for user \"{user.Id}\"");
                return true;
            }

            return false;
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
    /// Retrieves all users from the database, with the option to limit the number of results.
    /// </summary>
    /// <param name="limit">Maximum number of users to retrieve (0 for no limit).</param>
    /// <returns>A <see cref="ConcurrentBag{T}"/> containing the retrieved users.</returns>
    public async Task<ConcurrentBag<User>> GetAllAsync(int limit = 0)
    {
        ConcurrentBag<User> users = new ConcurrentBag<User>();

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when getting all users.", severity: Severity.Error);
            return users;
        }

        try
        {
            string selectSql = limit > 0
                ? $"SELECT id FROM {UsersTable} LIMIT @limit"
                : $"SELECT id FROM {UsersTable}";

            using var reader = limit > 0
                ? await _database.ExecuteReaderAsync(selectSql, new NpgsqlParameter("@limit", limit))
                : await _database.ExecuteReaderAsync(selectSql);

            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt64("id");
                // Fetch each user individually. This can be optimized with a JOIN if needed.
                var user = await FetchAsync((ulong)userId);
                if (user != null)
                {
                    users.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("GetAllUsersError", $"Error retrieving all users: {ex.Message}", severity: Severity.Error);
        }

        return users;
    }

    /// <summary>
    /// Deletes a user from the database permanently.
    /// </summary>
    /// <param name="id">Unique ID of the user to be deleted.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(ulong id)
    {
        if (!_database.IsConnected()) return false;

        try
        {
            string deleteSql = $"DELETE FROM {UsersTable} WHERE id = @id";
            var param = new NpgsqlParameter("@id", (long)id);

            int rowsAffected = await _database.ExecuteNonQueryAsync(deleteSql, param);

            if (rowsAffected > 0)
            {
                // Also remove from Redis cache
                await DeleteCache(id);
                await AresLogger.LogAsync("Repo: User", $"Deleted user \"{id}\" from database");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("DeleteUserError", $"Error deleting user {id}: {ex.Message}", severity: Severity.Error);
            return false;
        }
    }
}