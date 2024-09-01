using Discord_OpenAI.Backend.Database.MongoDB;
using Discord_OpenAI.Data;
using Discord_OpenAI.Manager;
using MongoDB.Bson;
using Newtonsoft.Json;
using MongoDB.Driver;
using Discord_OpenAI.Util.Extra;
using System.Collections.Concurrent;

namespace Discord_OpenAI.Backend.Data
{
    internal class GuildData
    {
        private readonly IMongoCollection<BsonDocument> collection;

        private readonly GuildManager? manager;

        public GuildData(MongoDatabase database)
        {
            this.collection = database.mongoDatabase.GetCollection<BsonDocument>("guilds");

            this.manager = Core.GuildManager;

            CreateIndexes();
        }

        public async Task CreateIndexes()
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("id");
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeys);
            await collection.Indexes.CreateManyAsync(new List<CreateIndexModel<BsonDocument>> { indexModel });
        }

        public async Task<Guild?> Save(string guildId)
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", guildId);
            BsonDocument element = await collection.Find(filter).FirstOrDefaultAsync();

            Guild guild = new Guild(guildId);

            if (element != null)
            {
                guild = JsonConvert.DeserializeObject<Guild>(element.ToJson());
            }
            else
            {
                var document = BsonDocument.Parse(JsonConvert.SerializeObject(guild));

                await collection.InsertOneAsync(document);
                manager.Save(guild);
            }

            return guild;
        }

        public async Task<Guild?> Fetch(string guildId)
        {
            Guild? guild = manager.Fetch(guildId);

            if (guild == null)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", guildId);
                BsonDocument element = await collection.Find(filter).FirstOrDefaultAsync();

                if (element != null)
                {
                    guild = JsonConvert.DeserializeObject<Guild>(element.ToJson());
                }
            }

            return guild;
        }

        public async Task Update(Guild guild, string field)
        {
            try
            {
                BsonDocument tree = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
                BsonElement valueElement;

                BsonValue? value = tree.TryGetElement(field, out valueElement) ? BsonValue.Create(valueElement.ToString()) : null;

                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("id", guild.Id);
                BsonDocument element = await collection.Find(filter).FirstOrDefaultAsync();

                if (element != null)
                {
                    var update = value != null ? Builders<BsonDocument>.Update.Set(field, value) : Builders<BsonDocument>.Update.Unset(field);
                    await collection.UpdateOneAsync(filter, update);
                }
            }
            catch (Exception e)
            {
                LogUtil.Error(e.Source, "Unable to save data:", e.Message);
            }
        }

        public void Cache(string id)
        {
            if (manager != null)
                manager.Delete(id);
        }

        public ConcurrentBag<Guild> GetGuilds(int limit = 0)
        {
            var documents = collection.Find(new BsonDocument()).Limit(limit).ToList();
            var accounts = new ConcurrentBag<Guild>();

            documents.ForEach(document => JsonConvert.DeserializeObject(document.ToJson()));

            return accounts;
        }
    }
}