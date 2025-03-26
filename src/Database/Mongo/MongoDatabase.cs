using Ares.src.Util;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Ares.src.Database.Mongo;

internal class MongoDatabase : DatabaseTemplate
{
    private static readonly string PATTERN = "([01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])";
    private static readonly Regex IP_PATTERN = new Regex(PATTERN + "\\." + PATTERN + "\\." + PATTERN + "\\." + PATTERN);

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

        this.url = IP_PATTERN.Match(credential.Host).Success
            ? "mongodb://" + (credential.User == null ? "" : credential.User + ":" + credential.Password + "@")
            + credential.Host + "/" + credential.Database
            + "?retryWrites=true&w=majority"
            : "mongodb+srv://" + (credential.User == null || string.IsNullOrEmpty(credential.User) ? "" : credential.User + ":" + credential.Password + "@")
            + credential.Host + "/"
            + credential.Database + "?retryWrites=true&w=majority";

        credentials = credential;
    }

    public void Connect()
    {
        long start = TimeUtil.CurrentTimeMillis();

        AresLogger.Log("MongoDB", "Connecting...");

        try
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(url);

            client = new MongoClient(settings);
            mongoDatabase = client.GetDatabase(credentials.Database);

            AresLogger.Log("MongoDB", $"Connection established successfully. ({FormatterUtil.FormatSeconds(start)})");
        }
        catch (Exception e)
        {
            AresLogger.Error("MongoDB", "Unable to connect...", e.Message);
        }
    }

    public void Close()
    {
        if (client != null)
        {
            client = null;
        }
    }

    public bool IsConnected()
    {
        return client != null;
    }
}