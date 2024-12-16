using Ares.src.Guild.ChatData;
using Ares.src.Guild.Information;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Utils.Extra;
using Discord;
using OpenAI.Chat;
using OpenAI.Images;

namespace Ares.src.Guild
{
    public class Guild
    {
        public readonly string Id;

        public GuildInformation Information;

        /// <summary>
        /// Construtor da classe Guild.
        /// </summary>
        /// <param name="id">Identificador da guilda.</param>

        public Guild(string id)
        {
            this.Id = id;
            this.Information = new GuildInformation();
        }

        /// <summary>
        /// Salva os campos especificados na guilda.
        /// </summary>
        /// <param name="fields">Lista de campos a serem salvos.</param>
        /// <returns>Retorna true se os campos foram salvos com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveAsync(List<string> fields)
        {
            if (fields == null || fields.Count == 0)
                throw new ArgumentException("A lista de campos não pode ser nula ou vazia.", nameof(fields));

            if (Core.GuildData is not { } guildData)
            {
                LogUtil.Error(nameof(SaveAsync), "GuildData está nulo. Não foi possível salvar os campos.");
                return false;
            }

            try
            {
                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field))
                        throw new ArgumentException("A lista de campos contém um valor nulo ou vazio.", nameof(fields));

                    await guildData.Update(this, field);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(SaveAsync), "Erro ao atualizar um ou vários campos no banco de dados.", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Salva um único campo especificado na guilda.
        /// </summary>
        /// <param name="field">Campo a ser salvo.</param>
        /// <returns>Retorna true se o campo foi salvo com sucesso, false caso contrário.</returns>
        
        public async Task<bool> SaveAsync(string field)
        {
            return await SaveAsync(new List<string> { field });
        }

        /// <summary>
        /// Salva as informações gerais da guilda.
        /// </summary>
        /// <param name="information">Objeto com as informações da guilda.</param>
        /// <returns>Retorna true se as informações foram salvas com sucesso, false caso contrário.</returns>
        
        public async Task<bool> SaveInformation(GuildInformation information)
        {
            if (information == null)
            {
                LogUtil.Error("InformationNull", "Não foi possível pegar as informações da guilda. (SaveInformation)");
                return false;
            }

            this.Information = information;

            return await SaveAsync("Information");
        }
        /// <summary>
        /// Atualiza os dados de chat da guilda no banco de dados.
        /// </summary>
        /// <param name="data">Objeto contendo os dados de chat da guilda.</param>
        /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveGuildChatDataAsync(GuildChatData data)
        {
            GuildInformation updatedInformation = Information;
            updatedInformation.GuildChatData = data;

            return await SaveInformation(updatedInformation);
        }

        /// <summary>
        /// Atualiza os dados de ID da guilda no banco de dados.
        /// </summary>
        /// <param name="data">Objeto contendo os dados de ID da guilda.</param>
        /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>

        public async Task<bool> SaveGuildIdDataAsync(GuildIdData data)
        {
            GuildInformation updatedInformation = Information;
            updatedInformation.GuildIdData = data;

            return await SaveInformation(updatedInformation);
        }

        /** Sistema de Conversa */

        /// <summary>
        /// Retorna o histórico de conversas da guilda.
        /// </summary>
        /// <returns>Dicionário contendo os históricos de conversas ou null caso não existam.</returns>

        public Dictionary<ulong, List<ChatMessage>>? ConversationHistorics()
        {
            GuildChatData? gcd = Information.GuildChatData;

            return (gcd != null ? gcd.ConversationHistorics : null);
        }

        /// <summary>
        /// Retorna o histórico de conclusões da guilda.
        /// </summary>
        /// <returns>Dicionário contendo os históricos de conclusões ou null caso não existam.</returns>

        public Dictionary<ulong, List<ChatCompletion>>? CompletionHistorics()
        {
            GuildChatData? gcd = Information.GuildChatData;

            return (gcd != null ? gcd.CompletionHistorics : null);
        }

        public Dictionary<ulong, List<GeneratedImage>>? GeneratedImageHistorics()
        {
            GuildChatData? gcd = Information.GuildChatData;

            return (gcd != null ? gcd.GeneratedImageHistorics : null);
        }

        /// <summary>
        /// Obtém as mensagens associadas a um usuário.
        /// </summary>
        /// <param name="user">Usuário para o qual buscar as mensagens.</param>
        /// <returns>Lista de mensagens ou null se não houver histórico para o usuário.</returns>

        public List<ChatMessage>? Messages(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var messages = ConversationHistorics();

            if (messages == null)
            {
                LogUtil.Log(nameof(Messages), "ConversationHistorics está nulo. Não foi possível recuperar as mensagens.");
                return null;
            }

            return messages.GetValueOrDefault(user.Id);
        }

        /// <summary>
        /// Cria dados de chat para um usuário.
        /// </summary>
        /// <param name="user">Usuário alvo.</param>
        /// <param name="model">Modelo a ser associado ao usuário.</param>
        /// <returns>Retorna true se os dados foram criados com sucesso, false caso contrário.</returns>

        public async Task<bool> CreateChatDataAsync(IUser user, OpenAiModel model)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (HasUserConversation(user) || HasUserConversationModel(user))
            {
                LogUtil.Log(nameof(CreateChatDataAsync), "O usuário já possui uma conversa ou modelo. Nenhuma ação necessária.");
                return false;
            }

            var gcd = Information.GuildChatData;
            if (gcd == null)
            {
                LogUtil.Error(nameof(CreateChatDataAsync), "GuildChatData está nulo. Não foi possível criar dados de chat para o usuário.");
                return false;
            }

            try
            {
                bool success =
                    gcd.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                    gcd.ConversationModels.TryAdd(user.Id, model);

                if (!success)
                {
                    LogUtil.Error(nameof(CreateChatDataAsync), "Falha ao adicionar o usuário nos dados de chat.");
                    return false;
                }

                return await this.SaveGuildChatDataAsync(gcd);

            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(CreateChatDataAsync), "Erro ao tentar criar dados de chat para o usuário.", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Remove os dados de chat de um usuário.
        /// </summary>
        /// <param name="user">Usuário alvo.</param>
        /// <returns>Retorna true se os dados foram removidos com sucesso, false caso contrário.</returns>

        public async Task<bool> DeleteChatDataAsync(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (!HasUserConversation(user) && !HasUserConversationModel(user))
                return false;

            if (Information.GuildChatData is not { } gcd)
            {
                LogUtil.Error(nameof(DeleteChatDataAsync), "GuildChatData está nulo. Não foi possível deletar os dados de chat.");
                return false;
            }

            gcd.ConversationHistorics.Remove(user.Id);
            gcd.ConversationModels.Remove(user.Id);

            try
            {
                return await this.SaveGuildChatDataAsync(gcd);
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(DeleteChatDataAsync), "Erro ao salvar as alterações após deletar os dados de chat.", ex.Message);
                return false;
            }
        }

        
        public async Task<bool> UpdateCompletionDataAsync(IUser user, List<ChatCompletion> completions)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (completions == null || completions.Count == 0)
                throw new ArgumentException("A lista de conclusões não pode ser nula ou vazia.", nameof(completions));

            if (Information.GuildChatData is not { } gcd)
            {
                LogUtil.Error(nameof(UpdateCompletionDataAsync), "GuildChatData está nulo. Não foi possível adicionar os dados de conclusão.");
                return false;
            }

            if (HasUserCompletion(user))
            {
                gcd.CompletionHistorics[user.Id] = completions;
            }
            else
            {
                gcd.CompletionHistorics.Add(user.Id, completions);
            }

            try
            {
                GuildInformation updatedInformation = Information;
                updatedInformation.GuildChatData = gcd;

                return await SaveInformation(updatedInformation);
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(UpdateCompletionDataAsync), "Erro ao salvar GuildChatData após adicionar os dados de conclusão.", ex.Message);
                return false;
            }
        }

        public async Task<bool> AddCompletionAsync(IUser user, ChatCompletion completion)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (completion == null) throw new ArgumentNullException(nameof(completion));

            var completionHistorics = CompletionHistorics();
            if (completionHistorics == null)
            {
                LogUtil.Error(nameof(AddCompletionAsync), "CompletionHistorics está nulo. Não foi possível adicionar a conclusão.");
                return false;
            }

            if (!completionHistorics.TryGetValue(user.Id, out var completions))
            {
                LogUtil.Log(nameof(AddCompletionAsync), $"Nenhum histórico encontrado para o usuário {user.Id}. Criando um novo.");

                completions = new List<ChatCompletion>();
                completionHistorics[user.Id] = completions;
            }

            completions.Add(completion);
            return await UpdateCompletionDataAsync(user, completions);
        }

        public async Task<bool> UpdateConversationAsync(IUser user, List<ChatMessage> messages)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            if (Information.GuildChatData is not { } gcd)
                return false;

            gcd.ConversationHistorics[user.Id] = messages;

            Information.GuildChatData = gcd;
            return await SaveInformation(Information);
        }

        public async Task<bool> AddConversationAsync(IUser user, ChatMessage message)
        {
            var conversationHistorics = this.ConversationHistorics();

            if (conversationHistorics == null)
            {
                LogUtil.Error(nameof(this.UpdateConversationAsync), "Conversation historics are null.");
                return false;
            }

            if (!conversationHistorics.TryGetValue(user.Id, out var messages) || messages == null)
            {
                LogUtil.Error(nameof(this.UpdateConversationAsync), $"Cannot retrieve chat messages for user ID {user.Id}.");
                return false;
            }

            messages.Add(message);

            return await this.UpdateConversationAsync(user, messages);
        }

        public async Task<bool> UpdateGeneratedImageAsync(IUser user, List<GeneratedImage> images)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (images == null) throw new ArgumentNullException(nameof(images));

            if (Information.GuildChatData is not { } gcd)
                return false;

            gcd.GeneratedImageHistorics[user.Id] = images;

            Information.GuildChatData = gcd;
            return await SaveInformation(Information);
        }

        public async Task<bool> AddGeneratedImageAsync(IUser user, GeneratedImage image)
        {
            var historic = this.GeneratedImageHistorics();

            if (historic == null)
            {
                LogUtil.Error(nameof(historic), "Generated Images historics are null.");
                return false;
            }

            if (!historic.TryGetValue(user.Id, out var images) || images == null)
            {
                LogUtil.Error(nameof(historic), $"Cannot retrieve generated images for user ID {user.Id}.");
                return false;
            }

            images.Add(image);

            return await this.UpdateGeneratedImageAsync(user, images);
        }

        public async Task<bool> RemoveConversationAsync(IUser user, ChatMessage message)
        {
            var conversationHistorics = this.ConversationHistorics();

            if (conversationHistorics == null)
            {
                LogUtil.Error(nameof(this.UpdateConversationAsync), "Conversation historics are null.");
                return false;
            }

            if (!conversationHistorics.TryGetValue(user.Id, out var messages) || messages == null)
            {
                LogUtil.Error(nameof(this.UpdateConversationAsync), $"Cannot retrieve chat messages for user ID {user.Id}.");
                return false;
            }

            messages.Remove(message);

            return await this.UpdateConversationAsync(user, messages);
        }

        /// <summary>
        /// Verifica se o usuário possui uma conversa existente.
        /// </summary>
        /// <param name="user">Usuário alvo.</param>
        /// <returns>Retorna true se a conversa existe, caso contrário, false.</returns>

        public bool HasUserConversation(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historics = ConversationHistorics();

            if (historics == null)
            {
                LogUtil.Error(nameof(HasUserConversation), "Não foi possível obter o histórico de conversas.");
                return false;
            }

            return historics.ContainsKey(user.Id);
        }

        public bool HasUserCompletion(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historics = CompletionHistorics();

            if (historics == null)
            {
                LogUtil.Error(nameof(HasUserCompletion), "Não foi possível obter o histórico de completações.");
                return false;
            }

            return historics.ContainsKey(user.Id);
        }

        public bool HasUserGeneratedImage(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var historics = GeneratedImageHistorics();

            if (historics == null)
            {
                LogUtil.Error(nameof(HasUserCompletion), "Não foi possível obter o histórico de iamges.");
                return false;
            }

            return historics.ContainsKey(user.Id);
        }

        public Dictionary<ulong, OpenAiModel> ConversationModels()
        {
            if (Information.GuildChatData == null)
            {
                LogUtil.Error(nameof(ConversationModels), "GuildChatData está nulo. Não foi possível obter os modelos de conversa.");
                return new Dictionary<ulong, OpenAiModel>();
            }

            return Information.GuildChatData.ConversationModels ?? new Dictionary<ulong, OpenAiModel>();
        }

        public bool AddModel(IUser user, OpenAiModel model)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (Information.GuildChatData is not { } guildChatData)
            {
                LogUtil.Error(nameof(AddModel), "GuildChatData está nulo. Não foi possível adicionar o modelo.");
                return false;
            }

            return guildChatData.ConversationModels.TryAdd(user.Id, model);
        }

        public OpenAiModel? GetModelByUser(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var conversationModels = ConversationModels();
            return conversationModels.TryGetValue(user.Id, out var model) ? model : null;
        }

        /// <summary>
        /// Verifica se o usuário possui um modelo associado.
        /// </summary>
        /// <param name="user">Usuário alvo.</param>
        /// <returns>Retorna true se o modelo existe, caso contrário, false.</returns>

        public bool HasUserConversationModel(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return ConversationModels().ContainsKey(user.Id);
        }

        public int GetTotalMessagesByRoleChat(IUser user, ChatMessageRole role)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (CompletionHistorics()?.TryGetValue(user.Id, out var messages) == true)
            {
                return messages.Count(m => m.Role == role);
            }

            return 0;
        }
    }
}