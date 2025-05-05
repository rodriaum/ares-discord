/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Manager;
using Ares.Core.Models;
using Ares.Core.Monitor;
using Ares.Core.Repository;
using Ares.Core.Service;
using DotNetEnv;
using Microsoft.Extensions.AI;

namespace Ares.Core;

/// <summary>
/// It has the function of managing all types of data,
/// as well as offering a variety of usable features.
/// </summary>
public class AresCore
{
    /// <summary>
    /// Gets or sets the Ollama client instance.
    /// </summary>
    public static IChatClient? OllamaClient { get; private set; }

    /// <summary>
    /// Gets or sets the MongoDB database instance.
    /// </summary>
    public static MongoService? MongoService { get; private set; }

    /// <summary>
    /// Gets or sets the Redis database instance.
    /// </summary>
    public static RedisService? RedisService { get; private set; }

    /// <summary>
    /// Gets or sets the guild collection for database operations.
    /// </summary>
    public static GuildRepository? GuildRepository { get; private set; }

    public static UserRepository? UserRepository { get; private set; }

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
        if (AresConstant.AppMonitorDebugMode)
        {
            SystemMonitor monitor = new SystemMonitor();
            _ = monitor.Init();
        }

        Uri? ollamaUri = new Uri($"http://{Env.GetString("OLLAMA_HOST")}:{Env.GetInt("OLLAMA_PORT")}");
        OllamaClient = new OllamaChatClient(ollamaUri);

        await LangManager.Init();
        return await InitDatabase();
    }

    public static async Task Close()
    {
        if (MongoService == null || RedisService == null)
            return;

        await MongoService.CloseAsync();
        await RedisService.CloseAsync();
    }

    public static bool IsDeveloper(ulong userId)
    {
        return AresConstant.DeveloperUserIds.Any(id => id.Equals(userId.ToString()));
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

        MongoService mongoDatabase = new MongoService(new DatabaseCredentials
        {
            Host = Env.GetString("MONGO_HOST"),
            User = Env.GetString("MONGO_USERNAME"),
            Database = Env.GetString("MONGO_DATABASE"),
            Password = Env.GetString("MONGO_PASSWORD"),
            Port = Env.GetInt("MONGO_PORT"),
        });

        await mongoDatabase.ConnectAsync();
        MongoService = mongoDatabase;

        /*
         * Redis connection
         */

        RedisService redisDatabase = new RedisService(new DatabaseCredentials
        {
            Host = Env.GetString("REDIS_HOST"),
            Password = Env.GetString("REDIS_PASSWORD"),
            Port = Env.GetInt("REDIS_PORT"),
        });

        await redisDatabase.ConnectAsync();
        RedisService = redisDatabase;

        /*
         * Database collections
         */

        GuildRepository = new GuildRepository(mongoDatabase, redisDatabase);
        UserRepository = new UserRepository(mongoDatabase, redisDatabase);

        return mongoDatabase != null && redisDatabase != null;
    }
}