using Ares.Util;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Ares.Database.Mongo;

internal class MongoDatabase : DatabaseTemplate
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

        this.url = _ipPattern.Match(credential.Host).Success
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

        try
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(url);

            client = new MongoClient(settings);
            mongoDatabase = client.GetDatabase(credentials.Database);

            await AresLogger.LogAsync("DB: Mongo", $"Connection established successfully. ({FormatterUtil.FormatSeconds(start)})");
        }
        catch (Exception e)
        {
            await AresLogger.ErrorAsync("DB: Mongo", "Unable to connect...", e.Message);
        }
    }

    public Task CloseAsync()
    {
        if (client != null)
        {
            client.Dispose();
            return Task.CompletedTask;
        }

        return Task.FromResult(false);
    }

    public bool IsConnected()
    {
        return client != null;
    }
}