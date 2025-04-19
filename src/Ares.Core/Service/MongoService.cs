/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Interfaces;
using Ares.Core.Util;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Ares.Core.Models;

namespace Ares.Core.Service;

public class MongoService : IDatabase
{
    private static readonly string _pattern = "([01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])";
    private static readonly Regex _ipPattern = new Regex(_pattern + "\\." + _pattern + "\\." + _pattern + "\\." + _pattern);

    private readonly DatabaseCredentials credentials;

    private readonly string url;

    private MongoClient? client;
    public IMongoDatabase? mongoDatabase;

    public MongoService(DatabaseCredentials credential)
    {
        if (credential.Host == null)
        {
            throw new ArgumentException($"Host cannot be null ({nameof(MongoService)})");
        }

        url = _ipPattern.Match(credential.Host).Success
            ? "mongodb://" + (credential.User == null ? "" : credential.User + ":" + credential.Password + "@")
            + credential.Host + "/" + credential.Database
            + "?retryWrites=true&w=majority"
            : "mongodb+srv://" + (credential.User == null || string.IsNullOrEmpty(credential.User) ? "" : credential.User + ":" + credential.Password + "@")
            + credential.Host + "/"
            + credential.Database + "?retryWrites=true&w=majority";

        credentials = credential;
    }

    public async Task ConnectAsync()
    {
        long start = TimeUtil.CurrentTimeMillis();

        await AresLogger.LogAsync("DB: Mongo", "Connecting...");

        int time = 15;

        int currentTries = 1;
        int maxTries = 3;

        bool connected = false;

        while (!connected)
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.FromConnectionString(url);

                client = new MongoClient(settings);
                mongoDatabase = client.GetDatabase(credentials.Database);

                await AresLogger.LogAsync("DB: Mongo", $"Connection established. ({currentTries}x/{FormatterUtil.FormatSeconds(start)})");
                connected = true;
            }
            catch (Exception e)
            {
                await AresLogger.ErrorAsync("DB: Mongo", "Unable to connect.", e.Message);
                
                connected = false;
                currentTries++;

                if (currentTries > maxTries)
                {
                    await AresLogger.ErrorAsync("DB: Mongo", "Max tries reached, stopping connection attempts.");
                    Environment.Exit(1);
                    break;
                }

                await AresLogger.ErrorAsync("DB: Mongo", $"Trying to connect in {time}s...");
                await Task.Delay(time);
            }
        }
    }

    public Task CloseAsync()
    {
        if (client != null)
        {
            try
            {
                client.Dispose();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                AresLogger.Error("DB: Mongo", "Unable to close connection.", e.Message);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    public bool IsConnected()
    {
        return client != null;
    }
}