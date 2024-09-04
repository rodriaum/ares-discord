using Ares.Backend.Database.MongoDB;
using Ares.Manager;
using MongoDB.Bson;
using Newtonsoft.Json;
using MongoDB.Driver;
using Ares.Util.Extra;
using System.Collections.Concurrent;

namespace Ares.Backend.Data
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

        public async void CreateIndexes()
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Id");
            var indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

            await collection.Indexes.CreateManyAsync(new List<CreateIndexModel<BsonDocument>> { indexModel });
        }

        public async Task<Guild.Guild?> Save(string id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
            var element = await collection.Find(filter).FirstOrDefaultAsync();

            Guild.Guild? guild = new Guild.Guild(id);

            if (element != null)
            {
                try
                {
                    var bsonDocument = BsonTypeMapper.MapToDotNetValue(element);
                    var jsonString = JsonConvert.SerializeObject(bsonDocument);
                    guild = JsonConvert.DeserializeObject<Guild.Guild>(jsonString);
                }
                catch (JsonReaderException ex)
                {
                    await LogUtil.ErrorAsync("JSON READER EXCEPTION", "Error deserializing document.", ex.Message);
                }
            }
            else
            {
                var document = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
                await collection.InsertOneAsync(document);
                manager.Save(guild);
            }

            return guild;
        }


        public async Task<Guild.Guild?> Fetch(string id)
        {
            Guild.Guild? guild = manager.Fetch(id);

            if (guild == null)
            {
                BsonDocument element = await collection.Find(Builders<BsonDocument>.Filter.Eq("Id", id)).FirstOrDefaultAsync();

                if (element != null)
                {
                    try
                    {
                        var bsonDocument = BsonTypeMapper.MapToDotNetValue(element);
                        var jsonString = JsonConvert.SerializeObject(bsonDocument);
                        guild = JsonConvert.DeserializeObject<Guild.Guild>(jsonString);
                    }
                    catch (JsonReaderException ex)
                    {
                        await LogUtil.ErrorAsync("JSON READER EXCEPTION", "Error deserializing document.", ex.Message);
                    }
                }
            }

            return guild;
        }

        public async Task<Guild.Guild?> Fetch(ulong id)
        {
            return await Fetch(id + "");
        }

        public async Task Update(Guild.Guild guild, string field)
        {
            try
            {
                BsonDocument tree = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
                BsonElement valueElement;

                BsonValue? value = tree.TryGetElement(field, out valueElement) ? BsonValue.Create(valueElement.ToString()) : null;

                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Id", guild.Id);

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

        public async Task<ConcurrentBag<Guild.Guild>> GetGuilds(int limit = 0)
        {
            var findOptions = new FindOptions<BsonDocument> { Limit = limit };
            var documents = await collection.FindAsync(new BsonDocument(), findOptions);
            var accounts = new ConcurrentBag<Guild.Guild>();

            await documents.ForEachAsync(async document =>
            {
                try
                {
                    var json = document.ToJson();
                    var bsonDocument = BsonTypeMapper.MapToDotNetValue(document);
                    var jsonString = JsonConvert.SerializeObject(bsonDocument);
                    var guild = JsonConvert.DeserializeObject<Guild.Guild>(jsonString);

                    if (guild != null)
                        accounts.Add(guild);
                }
                catch (JsonReaderException ex)
                {
                    await LogUtil.ErrorAsync("JSON READER EXCEPTION", "Error deserializing document.", ex.Message);
                }
            });

            return accounts;
        }
    }
}