using Ares.src.Backend.Database.Mongo;
using Ares.src.Guild;
using Ares.src.Manager;
using Ares.src.Utils.Extra;
using Ares.src;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;


using System.Collections.Concurrent;


/// <summary>
/// Classe responsável por gerenciar dados de guildas no banco de dados MongoDB.
/// </summary>
internal class GuildData
{
    /// <summary>
    /// Representa a coleção "guilds" no banco de dados MongoDB.
    /// </summary>
    private readonly IMongoCollection<BsonDocument>? _collection;

    /// <summary>
    /// Referência ao gerenciador de guildas usado para operações de cache e lógica relacionada.
    /// </summary>
    private readonly GuildManager _manager;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="GuildData"/> com a coleção de guildas e o gerenciador de guildas.
    /// </summary>
    /// <param name="database">Instância do banco de dados MongoDB que contém a coleção "guilds".</param>
    public GuildData(MongoDatabase database)
    {
        this._collection = database.mongoDatabase?.GetCollection<BsonDocument>("guilds");
        this._manager = Core.GuildManager;

        // Criação de índices na coleção para otimização de consultas.
        this.CreateIndexes();
    }

    /// <summary>
    /// Tenta estabelecer uma conexão com o MongoDB, verificando a conexão a cada 15 segundos
    /// caso a conexão falhe. A função continuará tentando até que a conexão seja bem-sucedida.
    /// </summary>
    /// <returns>Retorna true quando a conexão com o MongoDB for estabelecida com sucesso.</returns>
    public async Task<bool> WaitForMongoConnectionAsync()
    {
        var isConnected = false;

        while (!isConnected)
        {
            try
            {
                // Tenta enviar um comando ping para verificar a conexão.
                await this._collection?.Database.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }");
                isConnected = true;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ConnectionError", $"Failed to connect to MongoDB. Retrying in 15 seconds...", ex.Message);
                await Task.Delay(15000);
            }
        }

        return isConnected;
    }


    /// <summary>
    /// Cria índices na coleção "guilds" para melhorar a performance das consultas.
    /// </summary>
    public async void CreateIndexes()
    {
        await LogUtil.LogAsync("MongoDB", "Creating indexes in the database...");

        // Verifica se a coleção foi inicializada antes de tentar criar os índices.
        if (this._collection == null)
        {
            LogUtil.Error("CollectionNull", "Collection returned null when creating guild data indexes.");
            return;
        }

        // Chama a função para aguardar a conexão com o MongoDB.
        bool isConnected = await WaitForMongoConnectionAsync();

        if (isConnected)
        {
            // Após a conexão ser bem-sucedida, cria os índices.
            try
            {
                var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Id");
                var indexModel = new CreateIndexModel<BsonDocument>(indexKeys);

                await this._collection.Indexes.CreateManyAsync(new List<CreateIndexModel<BsonDocument>> { indexModel });

                await LogUtil.LogAsync("MongoDB", "Indexes created.");

            }
            catch (Exception ex)
            {
                LogUtil.Error("IndexCreationError", $"Error creating indexes: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// Salva ou atualiza uma guilda no banco de dados, retornando o objeto atualizado.
    /// </summary>
    /// <param name="id">ID único da guilda.</param>
    /// <returns>Objeto <see cref="Guild.Guild"/> representando a guilda salva ou atualizada.</returns>
    public async Task<Guild?> Save(string id)
    {
        if (this._collection == null)
        {
            LogUtil.Error("CollectionNull", "Collection returned null when save guild data.");
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
        var element = await this._collection.Find(filter).FirstOrDefaultAsync();

        Guild? guild = new Guild(id);

        if (element != null)
        {
            try
            {
                // Converte o documento BSON para JSON e desserializa para o objeto Guild.Guild.
                var document = BsonTypeMapper.MapToDotNetValue(element);
                var json = JsonConvert.SerializeObject(document);

                guild = JsonConvert.DeserializeObject<Guild>(json);
            }
            catch (JsonReaderException ex)
            {
                await LogUtil.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
            }
        }
        else
        {
            // Insere o documento no banco de dados caso não exista.
            var document = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
            await this._collection.InsertOneAsync(document);

            _manager.Save(guild);
        }

        return guild;
    }

    /// <summary>
    /// Recupera uma guilda do cache ou do banco de dados usando o ID.
    /// </summary>
    /// <param name="id">ID único da guilda.</param>
    /// <returns>Objeto <see cref="Guild.Guild"/> representando a guilda recuperada, ou null se não encontrada.</returns>
    public async Task<Guild?> Fetch(string id)
    {
        Guild? guild = _manager.Fetch(id);

        if (guild == null)
        {
            BsonDocument element = await _collection.Find(Builders<BsonDocument>.Filter.Eq("Id", id)).FirstOrDefaultAsync();

            if (element != null)
            {
                try
                {
                    // Converte o documento BSON para JSON e desserializa para o objeto Guild.Guild.
                    var document = BsonTypeMapper.MapToDotNetValue(element);
                    var json = JsonConvert.SerializeObject(document);

                    guild = JsonConvert.DeserializeObject<Guild>(json);
                }
                catch (JsonReaderException ex)
                {
                    await LogUtil.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
                }
            }
        }

        return guild;
    }

    /// <summary>
    /// Sobrecarga do método Fetch que aceita um ID numérico do tipo ulong.
    /// </summary>
    /// <param name="id">ID numérico da guilda.</param>
    /// <returns>Objeto <see cref="Guild.Guild"/> representando a guilda recuperada, ou null se não encontrada.</returns>
    public async Task<Guild?> Fetch(ulong id)
    {
        return await this.Fetch(id.ToString());
    }

    /// <summary>
    /// Atualiza um campo específico de uma guilda no banco de dados.
    /// </summary>
    /// <param name="guild">Objeto <see cref="Guild.Guild"/> representando a guilda a ser atualizada.</param>
    /// <param name="field">Nome do campo a ser atualizado.</param>
    public async Task<bool> Update(Guild guild, string field)
    {
        if (this._collection == null)
        {
            LogUtil.Error("CollectionNull", "Collection returned null when update guild data.");
            return false;
        }

        try
        {
            // Converte a guilda para BSON para manipulação no MongoDB.
            BsonDocument tree = BsonDocument.Parse(JsonConvert.SerializeObject(guild));
            BsonElement valueElement;

            // Obtém o valor do campo especificado, se existir.
            BsonValue? value = tree.TryGetElement(field, out valueElement) ? valueElement.Value : null;

            // Cria um filtro para localizar a guilda no banco de dados.
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Id", guild.Id);

            BsonDocument element = await _collection.Find(filter).FirstOrDefaultAsync();

            if (element != null)
            {
                // Define ou remove o campo no documento do banco de dados.
                var update = value != null ? Builders<BsonDocument>.Update.Set(field, value) : Builders<BsonDocument>.Update.Unset(field);
                await this._collection.UpdateOneAsync(filter, update);

                return true;
            }

        }
        catch (Exception e)
        {
            string? src = e.Source;

            LogUtil.Error((string.IsNullOrEmpty(src) ? "Exception" : src), "Unable to save data.", e.Message);
        }

        return false;
    }

    /// <summary>
    /// Remove uma guilda do cache local.
    /// </summary>
    /// <param name="id">ID único da guilda a ser removida do cache.</param>
    public void DeleteCache(string id)
    {
        _manager?.Delete(id);
    }

    /// <summary>
    /// Recupera todas as guildas do banco de dados, com a opção de limitar o número de resultados.
    /// </summary>
    /// <param name="limit">Número máximo de guildas a serem recuperadas (0 para sem limite).</param>
    /// <returns>Uma <see cref="ConcurrentBag{T}"/> contendo as guildas recuperadas.</returns>
    public async Task<ConcurrentBag<Guild>> GetGuilds(int limit = 0)
    {
        var accounts = new ConcurrentBag<Guild>();

        if (this._collection == null)
        {
            LogUtil.Error("CollectionNull", "Collection returned null when get all guilds.");
            return accounts;

        }

        var options = new FindOptions<BsonDocument> { Limit = limit };
        var documents = await _collection.FindAsync(new BsonDocument(), options);

        await documents.ForEachAsync(async document =>
        {
            try
            {
                // Converte o documento BSON para JSON e desserializa para o objeto Guild.Guild.
                var json = document.ToJson();
                var bsonDocument = BsonTypeMapper.MapToDotNetValue(document);
                var jsonString = JsonConvert.SerializeObject(bsonDocument);
                var guild = JsonConvert.DeserializeObject<Guild>(jsonString);

                if (guild != null)
                    accounts.Add(guild);
            }
            catch (JsonReaderException ex)
            {
                await LogUtil.ErrorAsync("JsonReaderException", "Error deserializing document.", ex.Message);
            }
        });

        return accounts;
    }
}