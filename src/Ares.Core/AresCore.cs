/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Backend.Database;
using Ares.Core.Database.Collection;
using Ares.Core.Database.Repository;
using Ares.Core.Manager;
using Ares.Core.Models.Database;
using Ares.Core.Monitor;
using Discord;
using DotNetEnv;
using Microsoft.Extensions.AI;

namespace Ares.Core;

/// <summary>
/// It has the function of managing all types of data,
/// as well as offering a variety of usable features.
/// </summary>
internal class AresCore
{
    /// <summary>
    /// Gets or sets the Ollama client instance.
    /// </summary>
    public static IChatClient? OllamaClient { get; private set; }

    /// <summary>
    /// Gets or sets the MongoDB database instance.
    /// </summary>
    public static MongoDatabase? MongoDatabase { get; private set; }

    /// <summary>
    /// Gets or sets the Redis database instance.
    /// </summary>
    public static RedisDatabase? RedisDatabase { get; private set; }

    /// <summary>
    /// Gets or sets the guild collection for database operations.
    /// </summary>
    public static GuildCollection? GuildCollection { get; private set; }

    /// <summary>
    /// Guild manager instance for handling guild-related operations.
    /// </summary>
    public static GuildManager GuildManager = new GuildManager();

    /// <summary>
    /// Language manager instance for handling localization.
    /// </summary>
    public static LangManager LangManager = new LangManager();

    /// <summary>
    /// Initializes the core components.
    /// </summary>
    /// <returns>True if the initialization was successful, otherwise false.</returns>
    public static async Task<bool> Init()
    {
        Uri ollamaUri = new Uri($"http://{Env.GetString("OLLAMA_HOST", fallback: "127.0.0.1")}:{Env.GetInt("OLLAMA_PORT", 11434)}");
        OllamaClient = new OllamaChatClient(ollamaUri);

        SystemMonitor monitor = new SystemMonitor();
        _ = monitor.Init();

        await LangManager.Init();
        return await InitDatabase();
    }

    public static async Task Close()
    {
        if (MongoDatabase == null || RedisDatabase == null)
            return;

        await MongoDatabase.CloseAsync();
        await RedisDatabase.CloseAsync();
    }

    public static bool IsDeveloper(IUser user)
    {
        return AresConstant.DeveloperUserIds.Any(id => id.Equals(user.Id.ToString()));
    }

    /// <summary>
    /// Initializes the database connection.
    /// </summary>
    /// <returns>True if the connection was successful, otherwise false.</returns>
    private static async Task<bool> InitDatabase()
    {
        /*
         * MongoDB connection 
         */

        MongoDatabase mongoDatabase = new MongoDatabase(new DatabaseCredentials
        {
            Host = Env.GetString("MONGO_HOST", fallback: "127.0.0.1"),
            User = Env.GetString("MONGO_USERNAME"),
            Database = Env.GetString("MONGO_DATABASE", fallback: "ares"),
            Password = Env.GetString("MONGO_PASSWORD"),
            Port = Env.GetInt("MONGO_PORT", fallback: 27017),
        });

        await mongoDatabase.ConnectAsync();
        MongoDatabase = mongoDatabase;

        /*
         * Redis connection
         */

        RedisDatabase redisDatabase = new RedisDatabase(new DatabaseCredentials
        {
            Host = Env.GetString("REDIS_HOST", fallback: "127.0.0.1"),
            Password = Env.GetString("REDIS_PASSWORD"),
            Port = Env.GetInt("REDIS_PORT", fallback: 6379),
        });

        await redisDatabase.ConnectAsync();
        RedisDatabase = redisDatabase;

        /*
         * Database collections
         */

        GuildCollection = new GuildCollection(mongoDatabase, redisDatabase);

        return mongoDatabase != null && redisDatabase != null;
    }
}