using Ares.Util;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ares.Database.Redis;

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
        this._credentials = credentials;
    }

    /// <summary>
    /// Initializes a new instance of the RedisDatabase with default localhost configuration.
    /// </summary>
    /// <remarks>
    /// Uses default host "localhost" and port 6379.
    /// </remarks>
    public RedisDatabase()
    {
        this._credentials = new DatabaseCredentials
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
    public void Connect()
    {
        long start = TimeUtil.CurrentTimeMillis();

        AresLogger.Log("Redis", "Starting connection to Redis...");

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

            this._connection = ConnectionMultiplexer.Connect(options);
            this._database = _connection.GetDatabase();

            AresLogger.Log("Redis", $"Redis connection successfully established. ({FormatterUtil.FormatSeconds(start)})");
        }
        catch (Exception ex)
        {
            AresLogger.Error("Redis", "Could not connect to Redis...", error: ex.Message);
        }
    }

    /// <summary>
    /// Closes and disposes of the active Redis connection.
    /// </summary>
    /// <remarks>
    /// Only closes the connection if it is currently established.
    /// </remarks>
    public void Close()
    {
        if (this.IsConnected())
        {
            this._connection?.Close();
            this._connection?.Dispose();
        }
    }

    /// <summary>
    /// Checks if a connection to Redis is currently active.
    /// </summary>
    /// <returns>True if connected, false otherwise.</returns>
    public bool IsConnected()
    {
        return this._connection != null && this._connection.IsConnected;
    }

    /// <summary>
    /// Completely clears all data from the current Redis database.
    /// </summary>
    public void Flush()
    {
        this._database.Execute("FLUSHDB");
    }

    /// <summary>
    /// Checks if a specific key exists in the Redis database.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public bool Exists(string key)
    {
        return this._database.KeyExists(key);
    }

    /// <summary>
    /// Saves an object to Redis if the key does not already exist.
    /// </summary>
    /// <param name="key">The key under which to store the object.</param>
    /// <param name="obj">The object to be saved.</param>
    public void Save(string key, object obj)
    {
        if (this.Exists(key)) return;

        HashEntry[] fields = ConvertToHashEntries(obj);
        this._database.HashSet(key, fields);
    }

    /// <summary>
    /// Updates an existing object in Redis.
    /// </summary>
    /// <param name="key">The key of the object to update.</param>
    /// <param name="obj">The updated object.</param>
    public void Update(string key, object obj)
    {
        if (Exists(key))
        {
            var fields = ConvertToHashEntries(obj);
            this._database.HashSet(key, fields);
        }
    }

    /// <summary>
    /// Publishes a message to a specific Redis channel.
    /// </summary>
    /// <param name="channel">The channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    public void Publish(string channel, string message)
    {
        this._connection.GetSubscriber().Publish(RedisChannel.Pattern(channel), message);
    }

    /// <summary>
    /// Saves an object to Redis with an expiration time.
    /// </summary>
    /// <param name="key">The key under which to store the object.</param>
    /// <param name="obj">The object to be saved.</param>
    /// <param name="expire">The number of seconds before the key expires.</param>
    public void Save(string key, object obj, int expire)
    {
        this.Save(key, obj);
        this.Cache(key, expire);
    }

    /// <summary>
    /// Deletes a key from the Redis database.
    /// </summary>
    /// <param name="key">The key to delete.</param>
    public void Delete(string key)
    {
        this._database.KeyDelete(key);
    }

    /// <summary>
    /// Sets an expiration time for a specific key.
    /// </summary>
    /// <param name="key">The key to set expiration for.</param>
    /// <param name="seconds">The number of seconds until expiration.</param>
    public void Cache(string key, int seconds)
    {
        this._database.KeyExpire(key, TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Removes the expiration time from a key, making it permanent.
    /// </summary>
    /// <param name="key">The key to make persistent.</param>
    public void Persist(string key)
    {
        if (this._database.KeyTimeToLive(key).HasValue)
        {
            this._database.KeyPersist(key);
        }
    }

    /// <summary>
    /// Deletes specific fields from a hash in Redis.
    /// </summary>
    /// <param name="key">The key of the hash.</param>
    /// <param name="fields">The fields to delete.</param>
    public void Delete(string key, params string[] fields)
    {
        this._database.HashDelete(key, fields.Select(f => (RedisValue)f).ToArray());
    }

    /// <summary>
    /// Loads an object from Redis by its key.
    /// </summary>
    /// <typeparam name="T">The type of object to load.</typeparam>
    /// <param name="key">The key of the object.</param>
    /// <returns>The loaded object, or null if not found.</returns>
    public T? Load<T>(string key) where T : class
    {
        HashEntry[] fields = this._database.HashGetAll(key);

        if (fields.Length == 0)
            return null;

        return this.ConvertFromHashEntries<T>(fields);
    }

    /// <summary>
    /// Loads all objects matching a specific key pattern.
    /// </summary>
    /// <typeparam name="T">The type of objects to load.</typeparam>
    /// <param name="key">The key pattern to match.</param>
    /// <returns>A list of matching objects.</returns>
    public List<T> LoadAll<T>(string key) where T : class
    {
        RedisResult keys = this._database.Execute("KEYS", $"{key}*");
        List<T> results = new List<T>();

        foreach (var value in keys.ToDictionary())
        {
            HashEntry[] fields = this._database.HashGetAll(value.Key);

            if (fields.Length > 0)
            {
                var obj = ConvertFromHashEntries<T>(fields);
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
    public void RemoveAll(string key)
    {
        RedisResult keys = this._database.Execute("KEYS", $"{key}*");

        foreach (var value in keys.ToDictionary())
        {
            this._database.KeyDelete(value.Key);
        }
    }

    /// <summary>
    /// Gets the total number of elements in a set.
    /// </summary>
    /// <param name="key">The key of the set.</param>
    /// <returns>The number of elements in the set.</returns>
    public int GetTotalCount(string key)
    {
        return (int)_database.SetLength(key);
    }

    /// <summary>
    /// Converts an object to Redis hash entries.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <returns>An array of hash entries.</returns>
    private HashEntry[] ConvertToHashEntries(object obj)
    {
        string json = JsonConvert.SerializeObject(obj);
        Dictionary<string, string>? dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (dictionary == null)
        {
            AresLogger.Error("Redis", "Could not convert object to hash entries.");
            return Array.Empty<HashEntry>();
        }

        return dictionary
            .Where(kvp => kvp.Value != null)
            .Select(kvp => new HashEntry(kvp.Key, kvp.Value))
            .ToArray();
    }

    /// <summary>
    /// Converts Redis hash entries to an object of specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to convert to.</typeparam>
    /// <param name="entries">The hash entries to convert.</param>
    /// <returns>The converted object, or null if conversion fails.</returns>
    private T? ConvertFromHashEntries<T>(HashEntry[] entries) where T : class
    {
        var dictionary = entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString()
        );

        string json = JsonConvert.SerializeObject(dictionary);

        return JsonConvert.DeserializeObject<T>(json);
    }
}