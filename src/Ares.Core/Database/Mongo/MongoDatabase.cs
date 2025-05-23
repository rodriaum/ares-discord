/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Interfaces;
using Ares.Core.Models.Database;
using Ares.Core.Objects;
using Ares.Core.Util;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Ares.Core.Database.Mongo;

public class MongoDatabase : IDatabase
{
    private static readonly string _pattern = "([01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])";
    private static readonly Regex _ipPattern = new Regex(_pattern + "\\." + _pattern + "\\." + _pattern + "\\." + _pattern);

    private readonly DatabaseCredentials credentials;

    private readonly string url;

    private MongoClient? client;
    public IMongoDatabase? mongoDatabase;

    public MongoDatabase(DatabaseCredentials credential)
    {
        if (credential.Host == null)
        {
            throw new ArgumentException($"Host cannot be null ({nameof(MongoDatabase)})");
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

        await AresLogger.LogAsync("DB: Redis", "Starting connection to Mongo...");

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

                await AresLogger.LogAsync("DB: Mongo", $"Connection established. ({currentTries}x/{FormatterUtil.FormatSeconds(start)})", severity: Severity.Success);
                connected = true;
            }
            catch (Exception e)
            {
                await AresLogger.LogAsync("DB: Mongo", "Unable to connect.", severity: Severity.Error, extra: e.Message);

                connected = false;
                currentTries++;

                if (currentTries > maxTries)
                {
                    await AresLogger.LogAsync("DB: Mongo", "Max tries reached, stopping connection attempts.", severity: Severity.Error);
                    Environment.Exit(1);
                    break;
                }

                await AresLogger.LogAsync("DB: Mongo", $"Trying to connect in {time}s...", severity: Severity.Error);
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
                AresLogger.Log("DB: Mongo", "Unable to close connection.", severity: Severity.Error, extra: e.Message);
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