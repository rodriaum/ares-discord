/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Database;
using Ares.Core.Database.Collection;
using Ares.Core.Database.Mongo;
using Ares.Core.Database.Redis;
using Ares.Core.Manager;
using System.Runtime.CompilerServices;

namespace Ares.Core;

/// <summary>
/// It has the function of managing all types of data,
/// as well as offering a variety of usable features.
/// </summary>
internal class AresCore
{
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
        await LangManager.Init();
        return await InitDatabase();
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
            Host = "127.0.0.1",
            Database = "ares",
            Port = 27017
        });

        await mongoDatabase.ConnectAsync();
        MongoDatabase = mongoDatabase;

        /*
         * Redis connection
         */

        RedisDatabase redisDatabase = new RedisDatabase(new DatabaseCredentials
        {
            Host = "127.0.0.1",
            Port = 6379
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