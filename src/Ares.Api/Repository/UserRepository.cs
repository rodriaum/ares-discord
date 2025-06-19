/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Database.Postgres;
using Ares.Core.Database.Redis;
using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Util;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;

namespace Ares.Core.Repository;

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

        // Create table and indexes to optimize queries.
        CreateTableAndIndexesAsync();
    }

    /// <summary>
    /// Creates the users table and indexes to improve query performance.
    /// </summary>
    public async void CreateTableAndIndexesAsync()
    {
        await AresLogger.LogAsync("Repo: User", "Creating table and indexes in the database...");

        if (!_database.IsConnected())
        {
            await AresLogger.LogAsync("DatabaseNotConnected", "Database connection is not available when creating user table.", severity: Severity.Error);
            return;
        }

        try
        {
            string createIndexSql = $@"
                CREATE INDEX IF NOT EXISTS idx_{UsersTable}_id ON {UsersTable} (id);
                CREATE INDEX IF NOT EXISTS idx_{UsersTable}_data_gin ON {UsersTable} USING GIN (data);";

            await _database.ExecuteNonQueryAsync(createIndexSql);

            await AresLogger.LogAsync("Repo: User", "Table and indexes created.");
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

        string selectSql = $"SELECT data FROM {UsersTable} WHERE id = @id";
        var selectParam = new NpgsqlParameter("@id", (long)id);

        try
        {
            string? userData = await _database.ExecuteScalarAsync<string>(selectSql, selectParam);
            User? user = new User(id);

            if (!string.IsNullOrEmpty(userData))
            {
                // User exists, deserialize from JSON
                user = JsonSerializer.Deserialize<User>(userData) ?? user;

                // Alert: Always set the id in case of security, if not set when deserialize
                user.Id = id;
            }
            else
            {
                // User doesn't exist, insert new user
                string userJson = JsonSerializer.Serialize(user);
                string insertSql = $@"
                    INSERT INTO {UsersTable} (id, data) 
                    VALUES (@id, @data::jsonb)";

                var insertParams = new NpgsqlParameter[]
                {
                    new("@id", (long)id),
                    new("@data", userJson)
                };

                await _database.ExecuteNonQueryAsync(insertSql, insertParams);
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
                string selectSql = $"SELECT data FROM {UsersTable} WHERE id = @id";
                var param = new NpgsqlParameter("@id", (long)id);

                string? userData = await _database.ExecuteScalarAsync<string>(selectSql, param);

                if (!string.IsNullOrEmpty(userData))
                {
                    user = JsonSerializer.Deserialize<User>(userData);

                    if (user != null)
                    {
                        // Alert: Always set the id in case of security, if not set when deserialize
                        user.Id = id;

                        if (saveInRedis)
                        {
                            await _redisDatabase.SaveAsync(GRedisKey + id, user);
                        }
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
            string userJson = JsonSerializer.Serialize(user);
            string updateSql = $@"
                UPDATE {UsersTable} 
                SET data = @data::jsonb, updated_at = CURRENT_TIMESTAMP 
                WHERE id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", (long)user.Id),
                new("@data", userJson)
            };

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
                ? $"SELECT data FROM {UsersTable} LIMIT @limit"
                : $"SELECT data FROM {UsersTable}";

            using var reader = limit > 0
                ? await _database.ExecuteReaderAsync(selectSql, new NpgsqlParameter("@limit", limit))
                : await _database.ExecuteReaderAsync(selectSql);

            while (await reader.ReadAsync())
            {
                try
                {
                    string userData = reader.GetString("data");
                    User? user = JsonSerializer.Deserialize<User>(userData);

                    if (user != null)
                        users.Add(user);
                }
                catch (JsonException ex)
                {
                    await AresLogger.LogAsync("JsonDeserializationError", "Error deserializing user data.", severity: Severity.Error, extra: ex.Message);
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