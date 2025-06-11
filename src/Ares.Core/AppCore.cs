/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Database.Postgres;
using Ares.Core.Database.Redis;
using Ares.Core.Manager.Lang;
using Ares.Core.Models.Database;
using Ares.Core.Monitor;
using Ares.Core.Repository;
using DotNetEnv;
using Microsoft.Extensions.AI;

namespace Ares.Core;

/// <summary>
/// It has the function of managing all types of data,
/// as well as offering a variety of usable features.
/// </summary>
public class AppCore
{
    /// <summary>
    /// Gets or sets the ollama client instance.
    /// </summary>
    public static IChatClient? OllamaClient { get; private set; }

    /// <summary>
    /// Gets or sets the postgres database instance.
    /// </summary>
    public static PostgresDatabase? PostgresDatabase { get; private set; }

    /// <summary>
    /// Gets or sets the redis database instance.
    /// </summary>
    public static RedisDatabase? RedisDatabase { get; private set; }

    /// <summary>
    /// Gets or sets the guild collection for database operations.
    /// </summary>
    public static GuildRepository? GuildRepository { get; private set; }

    /// <summary>
    /// Gets or sets the user collection for database operations.
    /// </summary>
    public static UserRepository? UserRepository { get; private set; }

    /// <summary>
    /// Gets or sets the model collection for database operations.
    /// </summary>
    public static ChatModelRepository? ChatModelRepository { get; private set; }

    /// <summary>
    /// Language manager instance for handling localization.
    /// </summary>
    public static LanguageManager LangManager = new LanguageManager();

    /// <summary>
    /// Initializes the core components.
    /// </summary>
    /// <returns>True if the initialization was successful, otherwise false.</returns>
    public static async Task<bool> Init()
    {
        if (AppConstants.AppMonitorDebugMode)
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
        if (PostgresDatabase == null || RedisDatabase == null)
            return;

        await PostgresDatabase.CloseAsync();
        await RedisDatabase.CloseAsync();
    }

    public static bool IsDeveloper(ulong userId)
    {
        return AppConstants.DeveloperUserIds.Any(id => id.Equals(userId.ToString()));
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

        PostgresDatabase postgresDatabase = new(new DatabaseCredentials
        {
            Host = Env.GetString("POSTGRES_HOST"),
            User = Env.GetString("POSTGRES_USER"),
            Database = Env.GetString("POSTGRES_DATABASE"),
            Password = Env.GetString("POSTGRES_PASSWORD"),
            Port = Env.GetInt("POSTGRES_PORT"),
        });

        await postgresDatabase.ConnectAsync();
        PostgresDatabase = postgresDatabase;

        /*
         * Redis connection
         */

        RedisDatabase redisDatabase = new(new DatabaseCredentials
        {
            Host = Env.GetString("REDIS_HOST"),
            Password = Env.GetString("REDIS_PASSWORD"),
            Port = Env.GetInt("REDIS_PORT"),
        });

        await redisDatabase.ConnectAsync();
        RedisDatabase = redisDatabase;

        /*
         * Database collections
         */

        GuildRepository = new(postgresDatabase, redisDatabase);
        UserRepository = new(postgresDatabase, redisDatabase);
        ChatModelRepository = new(postgresDatabase, redisDatabase);

        return postgresDatabase != null && redisDatabase != null;
    }
}