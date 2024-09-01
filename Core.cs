using Discord_OpenAI.Backend.Data;
using Discord_OpenAI.Backend.Database;
using Discord_OpenAI.Backend.Database.MongoDB;
using Discord_OpenAI.Data;
using Discord_OpenAI.Manager;
using Discord_OpenAI.Util.Extra;

namespace Discord_OpenAI
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
                Database = "discord_openai",
                Port = 27017
            });

            database.Connect();

            Database = database;

            GuildData = new GuildData(database);

            GuildManager = new GuildManager();

            string id = "1248820762123702315";

            Guild guild = await GuildData.Fetch(id);

            if (guild == null)
            {
                guild = await GuildData.Save(id);

                if (guild == null)
                {
                    LogUtil.Error("ACCOUNT", "Unable to save account for id \"" + id + "\"", "");
                }
                else
                {
                    LogUtil.Log("ACCOUNT", "New account created and saved: " + guild);
                }
            }

            GuildManager.Save(guild);
        }
    }
}
