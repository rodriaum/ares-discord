/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Core.Backend.Database;
using Ares.Core.Util;
using MongoDB.Driver.Linq;
using StackExchange.Redis;
using System.Text.Json;

namespace Ares.Ares.Core.Backend.Database.Redis;

/// <summary>
/// Represents a Redis database connection and provides methods for interacting with Redis.
/// </summary>
/// <remarks>
/// This class implements database operations using StackExchange.Redis library,
/// supporting connection management, data storage, retrieval, and caching.
/// </remarks>
public class RedisDatabase : DatabaseTemplate
{
    private readonly DatabaseCredentials _credentials;

    private ConnectionMultiplexer? _connection;
    private IDatabase? _database;

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
        long start = TimeUtil.CurrentTimeMillis();

        await AresLogger.LogAsync("DB: Redis", "Starting connection to Redis...");

        int time = 15;
        int tries = 1;
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

                await AresLogger.LogAsync("DB: Redis", $"Redis connection established. ({tries}x/{FormatterUtil.FormatSeconds(start)})");
                connected = true;
            }
            catch (Exception ex)
            {
                await AresLogger.ErrorAsync("DB: Redis", "Could not connect to Redis.", extra: ex.Message);

                connected = false;
                tries++;

                await AresLogger.ErrorAsync("DB: Redis", $"Trying to connect in {time}s...");
                await Task.Delay(time);
            }
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
        if (IsConnected())
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
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
    public async Task<RedisResult> FlushAsync()
    {
        return await _database.ExecuteAsync("FLUSHDB");
    }

    /// <summary>
    /// Checks if a specific key exists in the Redis database.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    /// <summary>
    /// Saves an object to Redis if the key does not already exist.
    /// </summary>
    /// <param name="key">The key under which to store the object.</param>
    /// <param name="obj">The object to be saved.</param>
    public async Task SaveAsync(string key, object obj)
    {
        if (await ExistsAsync(key)) return;

        HashEntry[] fields = await ConvertToHashEntriesAsync(obj);
        await _database.HashSetAsync(key, fields);
    }

    /// <summary>
    /// Updates an existing object in Redis.
    /// </summary>
    /// <param name="key">The key of the object to update.</param>
    /// <param name="obj">The updated object.</param>
    public async Task UpdateAsync(string key, object obj)
    {
        if (await ExistsAsync(key))
        {
            HashEntry[] fields = await ConvertToHashEntriesAsync(obj);
            await _database.HashSetAsync(key, fields);
        }
    }

    /// <summary>
    /// Publishes a message to a specific Redis channel.
    /// </summary>
    /// <param name="channel">The channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    public async Task<long> PublishAsync(string channel, string message)
    {
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
        await SaveAsync(key, obj);
        await CacheAsync(key, expire);
    }

    /// <summary>
    /// Deletes a key from the Redis database.
    /// </summary>
    /// <param name="key">The key to delete.</param
    public async Task<bool> DeleteAsync(string key)
    {
        return await _database.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Sets an expiration time for a specific key.
    /// </summary>
    /// <param name="key">The key to set expiration for.</param>
    /// <param name="seconds">The number of seconds until expiration.</param>
    public async Task CacheAsync(string key, int seconds)
    {
        await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Removes the expiration time from a key, making it permanent.
    /// </summary>
    /// <param name="key">The key to make persistent.</param>
    public async Task<bool> PersistAsync(string key)
    {
        if (await _database.KeyTimeToLiveAsync(key) != null)
        {
            return await _database.KeyPersistAsync(key);
        }

        return false;
    }

    /// <summary>
    /// Deletes specific fields from a hash in Redis.
    /// </summary>
    /// <param name="key">The key of the hash.</param>
    /// <param name="fields">The fields to delete.</param>
    public async Task<long> DeleteAsync(string key, params string[] fields)
    {
        return await _database.HashDeleteAsync(key, fields.Select(f => (RedisValue)f).ToArray());
    }

    /// <summary>
    /// Loads an object from Redis by its key.
    /// </summary>
    /// <typeparam name="T">The type of object to load.</typeparam>
    /// <param name="key">The key of the object.</param>
    /// <returns>The loaded object, or null if not found.</returns>
    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        HashEntry[] fields = await _database.HashGetAllAsync(key);

        if (fields.Length == 0)
            return null;

        return await ConvertFromHashEntriesAsync<T>(fields);
    }

    /// <summary>
    /// Loads all objects matching a specific key pattern.
    /// </summary>
    /// <typeparam name="T">The type of objects to load.</typeparam>
    /// <param name="key">The key pattern to match.</param>
    /// <returns>A list of matching objects.</returns>
    public async Task<List<T>> LoadAllAsync<T>(string key) where T : class
    {
        RedisResult keys = await _database.ExecuteAsync("KEYS", $"{key}*");
        List<T> results = new List<T>();

        foreach (KeyValuePair<string, RedisResult> value in keys.ToDictionary())
        {
            HashEntry[] fields = await _database.HashGetAllAsync(value.Key);

            if (fields.Length > 0)
            {
                T? obj = await ConvertFromHashEntriesAsync<T>(fields);
                if (obj != null)
                    results.Add(obj);
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
        RedisResult keys = await _database.ExecuteAsync("KEYS", $"{key}*");

        foreach (KeyValuePair<string, RedisResult> value in keys.ToDictionary())
        {
            await _database.KeyDeleteAsync(value.Key);
        }
    }

    /// <summary>
    /// Gets the total number of elements in a set.
    /// </summary>
    /// <param name="key">The key of the set.</param>
    /// <returns>The number of elements in the set.</returns>
    public async Task<int> GetTotalCountAsync(string key)
    {
        long lenght = await _database.SetLengthAsync(key);
        return (int)lenght;
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
            AresLogger.Error("DB: Redis", "Could not convert object to hash entries.");
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
}