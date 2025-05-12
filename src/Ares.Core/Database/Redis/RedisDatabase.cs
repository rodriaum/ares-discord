/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Models.Database;
using Ares.Core.Objects;
using Ares.Core.Util;
using MongoDB.Driver.Linq;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ares.Core.Database.Redis;

/// <summary>
/// Represents a Redis database connection and provides methods for interacting with Redis.
/// </summary>
/// <remarks>
/// This class implements database operations using StackExchange.Redis library,
/// supporting connection management, data storage, retrieval, and caching.
/// </remarks>
public class RedisDatabase : Interfaces.IDatabase
{
    private readonly DatabaseCredentials _credentials;

    private ConnectionMultiplexer? _connection;
    private IDatabase? _database;

    /// <summary>
    /// Dictionary of locks for concurrent operations on the same key
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    /// <summary>
    /// Lock for connection operations
    /// </summary>
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Initializes a new instance of the RedisDatabase with specified credentials.
    /// </summary>
    /// <param name="credentials">The database connection credentials.</param>
    public RedisDatabase(DatabaseCredentials credentials)
    {
        _credentials = credentials;
    }

    /// <summary>
    /// Initializes a new instance of the RedisDatabase with default localhost configuration.
    /// </summary>
    /// <remarks>
    /// Uses default host "localhost" and port 6379.
    /// </remarks>
    public RedisDatabase()
    {
        _credentials = new DatabaseCredentials
        {
            Host = "127.0.0.1",
            Port = 6379
        };
    }

    /// <summary>
    /// Establishes a connection to the Redis server using the provided credentials.
    /// </summary>
    /// <remarks>
    /// Configures connection options including timeout settings and authentication.
    /// Logs successful connection or any connection errors.
    /// </remarks>
    public async Task ConnectAsync()
    {
        try
        {
            await _connectionLock.WaitAsync();

            long start = TimeUtil.CurrentTimeMillis();

            await AresLogger.LogAsync("DB: Redis", "Starting connection to Redis...");

            int time = 15;

            int currentTries = 1;
            int maxTries = 3;

            bool connected = false;

            while (!connected)
            {
                try
                {
                    ConfigurationOptions options = new ConfigurationOptions
                    {
                        EndPoints = { $"{_credentials.Host}:{_credentials.Port}" },
                        Password = _credentials.Password,
                        ConnectTimeout = 5000,
                        SyncTimeout = 5000,
                        AsyncTimeout = 5000
                    };

                    _connection = await ConnectionMultiplexer.ConnectAsync(options);
                    _database = _connection.GetDatabase();

                    await AresLogger.LogAsync("DB: Redis", $"Redis connection established. ({currentTries}x/{FormatterUtil.FormatSeconds(start)})");
                    connected = true;
                }
                catch (Exception ex)
                {
                    await AresLogger.LogAsync("DB: Redis", "Could not connect to Redis.", extra: ex.Message, severity: Severity.Error);

                    connected = false;
                    currentTries++;

                    if (currentTries > maxTries)
                    {
                        await AresLogger.LogAsync("DB: Redis", "Max tries reached, stopping connection attempts.", severity: Severity.Error);
                        Environment.Exit(1);
                        break;
                    }

                    await AresLogger.LogAsync("DB: Redis", $"Trying to connect in {time}s...", severity: Severity.Error);
                    await Task.Delay(time);
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Closes and disposes of the active Redis connection.
    /// </summary>
    /// <remarks>
    /// Only closes the connection if it is currently established.
    /// </remarks>
    public async Task CloseAsync()
    {
        try
        {
            await _connectionLock.WaitAsync();

            if (IsConnected())
            {
                try
                {
                    if (await FlushAsync() != null)
                        await AresLogger.LogAsync("DB: Redis", "Redis database cache has been cleared.", severity: Severity.Debug);

                    await _connection?.CloseAsync()!;
                    await _connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    await AresLogger.LogAsync("DB: Redis", "Could not close connection.", severity: Severity.Error, extra: ex.Message);
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Checks if a connection to Redis is currently active.
    /// </summary>
    /// <returns>True if connected, false otherwise.</returns>
    public bool IsConnected()
    {
        return _connection != null && _connection.IsConnected;
    }

    /// <summary>
    /// Completely clears all data from the current Redis database.
    /// </summary>
    public async Task<RedisResult?> FlushAsync()
    {
        if (_database == null) return null;
        return await _database.ExecuteAsync("FLUSHDB");
    }

    /// <summary>
    /// Checks if a specific key exists in the Redis database.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        if (_database == null) return false;
        return await _database.KeyExistsAsync(key);
    }

    /// <summary>
    /// Saves an object to Redis if the key does not already exist.
    /// </summary>
    /// <param name="key">The key under which to store the object.</param>
    /// <param name="obj">The object to be saved.</param>
    public async Task SaveAsync(string key, object obj)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return;
            if (await ExistsAsync(key)) return;

            HashEntry[] fields = await ConvertToHashEntriesAsync(obj);
            await _database.HashSetAsync(key, fields);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Updates an existing object in Redis.
    /// </summary>
    /// <param name="key">The key of the object to update.</param>
    /// <param name="obj">The updated object.</param>
    public async Task UpdateAsync(string key, object obj)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return;

            if (await ExistsAsync(key))
            {
                HashEntry[] fields = await ConvertToHashEntriesAsync(obj);
                await _database.HashSetAsync(key, fields);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Publishes a message to a specific Redis channel.
    /// </summary>
    /// <param name="channel">The channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    public async Task<long> PublishAsync(string channel, string message)
    {
        if (_connection == null) return 0;
        return await _connection.GetSubscriber().PublishAsync(RedisChannel.Pattern(channel), message);
    }

    /// <summary>
    /// Saves an object to Redis with an expiration time.
    /// </summary>
    /// <param name="key">The key under which to store the object.</param>
    /// <param name="obj">The object to be saved.</param>
    /// <param name="expire">The number of seconds before the key expires.</param>
    public async Task SaveAsync(string key, object obj, int expire)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            await SaveAsync(key, obj);
            await CacheAsync(key, expire);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Deletes a key from the Redis database.
    /// </summary>
    /// <param name="key">The key to delete.</param
    public async Task<bool> DeleteAsync(string key)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return false;
            return await _database.KeyDeleteAsync(key);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Sets an expiration time for a specific key.
    /// </summary>
    /// <param name="key">The key to set expiration for.</param>
    /// <param name="seconds">The number of seconds until expiration.</param>
    public async Task CacheAsync(string key, int seconds)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return;
            await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(seconds));
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Removes the expiration time from a key, making it permanent.
    /// </summary>
    /// <param name="key">The key to make persistent.</param>
    public async Task<bool> PersistAsync(string key)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (await _database?.KeyTimeToLiveAsync(key)! != null)
            {
                return await _database.KeyPersistAsync(key);
            }

            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Deletes specific fields from a hash in Redis.
    /// </summary>
    /// <param name="key">The key of the hash.</param>
    /// <param name="fields">The fields to delete.</param>
    public async Task<long> DeleteAsync(string key, params string[] fields)
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return 0;
            return await _database.HashDeleteAsync(key, fields.Select(f => (RedisValue)f).ToArray());
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Loads an object from Redis by its key.
    /// </summary>
    /// <typeparam name="T">The type of object to load.</typeparam>
    /// <param name="key">The key of the object.</param>
    /// <returns>The loaded object, or null if not found.</returns>
    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await semaphore.WaitAsync();

            if (_database == null) return null;

            HashEntry[] fields = await _database.HashGetAllAsync(key);

            if (fields.Length == 0)
                return null;

            return await ConvertFromHashEntriesAsync<T>(fields);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Loads all objects matching a specific key pattern.
    /// </summary>
    /// <typeparam name="T">The type of objects to load.</typeparam>
    /// <param name="key">The key pattern to match.</param>
    /// <returns>A list of matching objects.</returns>
    public async Task<List<T>> LoadAllAsync<T>(string key) where T : class
    {
        if (_database == null) return new();

        RedisResult keys = await _database.ExecuteAsync("KEYS", $"{key}*");
        List<T> results = new List<T>();

        foreach (KeyValuePair<string, RedisResult> value in keys.ToDictionary())
        {
            var semaphore = _keyLocks.GetOrAdd(value.Key, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();

                HashEntry[] fields = await _database.HashGetAllAsync(value.Key);

                if (fields.Length > 0)
                {
                    T? obj = await ConvertFromHashEntriesAsync<T>(fields);
                    if (obj != null)
                        results.Add(obj);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        return results;
    }

    /// <summary>
    /// Removes all keys matching a specific pattern.
    /// </summary>
    /// <param name="key">The key pattern to match and remove.</param>
    public async Task RemoveAllAsync(string key)
    {
        if (_database == null) return;

        RedisResult keys = await _database.ExecuteAsync("KEYS", $"{key}*");

        foreach (KeyValuePair<string, RedisResult> value in keys.ToDictionary())
        {
            var semaphore = _keyLocks.GetOrAdd(value.Key, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();
                await _database.KeyDeleteAsync(value.Key);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Gets the total number of elements in a set.
    /// </summary>
    /// <param name="key">The key of the set.</param>
    /// <returns>The number of elements in the set.</returns>
    public async Task<long> GetTotalCountAsync(string key)
    {
        if (_database == null) return 0;

        return await _database.SetLengthAsync(key);
    }

    /// <summary>
    /// Converts an object to Redis hash entries asynchronously.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <returns>A Task representing a HashEntry array.</returns>
    private async Task<HashEntry[]> ConvertToHashEntriesAsync(object obj)
    {
        Dictionary<string, string>? dictionary = await JsonUtil.ObjectToDictionaryAsync(obj);

        if (dictionary == null)
        {
            AresLogger.Log("DB: Redis", "Could not convert object to hash entries.", severity: Severity.Error);
            return Array.Empty<HashEntry>();
        }

        return dictionary
            .Where(kvp => kvp.Value != null)
            .Select(kvp => new HashEntry(kvp.Key, kvp.Value))
            .ToArray();
    }

    /// <summary>
    /// Converts Redis hash entries to an object of specified type asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of object to convert to.</typeparam>
    /// <param name="entries">The hash entries to convert.</param>
    /// <returns>A Task representing the converted object, or null if conversion fails.</returns>
    private async Task<T?> ConvertFromHashEntriesAsync<T>(HashEntry[] entries) where T : class
    {
        Dictionary<string, string> dictionary = entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString()
        );

        return await JsonUtil.DictionaryToObjectAsync<T?>
            (
                dictionary,
                deserializeOptions: new JsonSerializerOptions { IncludeFields = true }
            );
    }

    /// <summary>
    /// Cleanup method to remove unused locks and free memory
    /// </summary>
    public void CleanupLocks(TimeSpan olderThan)
    {
        foreach (var key in _keyLocks.Keys)
        {
            if (_keyLocks.TryRemove(key, out var semaphore))
            {
                semaphore.Dispose();
            }
        }
    }
}