using Ares.Backend.Database;
using Ares.src.Backend.Data;
using Ares.src.Backend.Database.Mongo;
using Ares.src.Manager;

using Ares.src.Backend.Database;

namespace Ares.src
{
    internal class Core
    {
        // Databases
        public static MongoDatabase? Database { get; set; }

        public static GuildData? GuildData { get; set; }
        public static GuildManager? GuildManager = new GuildManager();

        public async static void Init()
        {
            MongoDatabase database = new MongoDatabase(new DatabaseCredentials
            {
                Host = "127.0.0.1",
                Database = "ares",
                Port = 27017
            });

            database.Connect();

            Database = database;

            GuildData = new GuildData(database);
            GuildManager = new GuildManager();
        }
    }
}
