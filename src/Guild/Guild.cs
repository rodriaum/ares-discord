using Ares.src.Guild.ChatData;
using Ares.src.Guild.Information;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Util.Extra;
using Discord;
using OpenAI.Chat;

namespace Ares.src.Guild
{
    public class Guild
    {
        public readonly string Id;

        public GuildInformation Information;

        public Guild(string id)
        {
            Id = id;

            this.Information = new GuildInformation(
                openAiToken: "",

                guildIdData: new GuildIdData
                {
                    MemberRoleId = 0L,
                    UsageRoleId = 0L,
                    ExclusiveRoleId = 0L,
                    SetupChannelId = 0L,
                    ChatsCategoryId = 0L
                },

                guildChatData: new GuildChatData
                {
                    ConversationModels = new Dictionary<ulong, OpenAiModel>(),
                    ConversationHistorics = new Dictionary<ulong, List<ChatMessage>>(),
                    CompletionHistorics = new Dictionary<ulong, List<ChatCompletion>>()
                }
            );
        }

        /// <summary>
        /// Save account fields.
        /// </summary>
        /// <param name="fields">Fields to save</param>

        public async Task<bool> Save(List<string> fields)
        {
            if (fields == null || fields.Count == 0)
                throw new ArgumentException("A lista de campos não pode ser nula ou vazia.", nameof(fields));

            if (Core.GuildData is not { } guildData)
            {
                LogUtil.Error(nameof(Save), "GuildData está nulo. Não foi possível salvar os campos.");
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
                LogUtil.Error(nameof(Save), "Erro ao atualizar um ou vários campos no banco de dados.", ex.Message);
                return false;
            }
        }

        public async Task<bool> Save(string field)
        {
            return await Save(new List<string> { field });
        }

        public async Task<bool> SaveInformation(GuildInformation information)
        {
            if (information == null)
            {
                LogUtil.Error("InformationNull", "Não foi possível pegar as informações da guilda. (SaveInformation)");
                return false;
            }

            this.Information = information;

            return await Save("Information");
        }

        public async Task<bool> SaveGuildChatData(GuildChatData data)
        {
            GuildInformation updatedInformation = Information;
            updatedInformation.GuildChatData = data;

            return await SaveInformation(updatedInformation);
        }

        public async Task<bool> SaveGuildIdData(GuildIdData data)
        {
            GuildInformation updatedInformation = Information;
            updatedInformation.GuildIdData = data;

            return await SaveInformation(updatedInformation);
        }

        /** Conversation System */

        public Dictionary<ulong, List<ChatMessage>>? ConversationHistorics()
        {
            GuildChatData? gcd = Information.GuildChatData;

            return (gcd != null ? gcd.ConversationHistorics : null);
        }

        public Dictionary<ulong, List<ChatCompletion>>? CompletionHistorics()
        {
            GuildChatData? gcd = Information.GuildChatData;

            return (gcd != null ? gcd.CompletionHistorics : null);
        }

        public List<ChatMessage>? Messages(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var completionHistorics = CompletionHistorics();

            if (completionHistorics == null)
            {
                LogUtil.Log(nameof(Messages), "CompletionHistorics está nulo. Não foi possível recuperar as mensagens.");
                return null;
            }

            return ConversationHistorics()?.GetValueOrDefault(user.Id);
        }

        public async Task<bool> CreateChatData(IUser user, OpenAiModel model)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (HasUserConversation(user) || HasUserConversationModel(user))
            {
                LogUtil.Log(nameof(CreateChatData), "O usuário já possui uma conversa ou modelo. Nenhuma ação necessária.");
                return false;
            }

            var gcd = Information.GuildChatData;
            if (gcd == null)
            {
                LogUtil.Error(nameof(CreateChatData), "GuildChatData está nulo. Não foi possível criar dados de chat para o usuário.");
                return false;
            }

            try
            {
                bool success =
                    gcd.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                    gcd.ConversationModels.TryAdd(user.Id, model);

                if (!success)
                {
                    LogUtil.Error(nameof(CreateChatData), "Falha ao adicionar o usuário nos dados de chat.");
                    return false;
                }

                return await this.SaveGuildChatData(gcd);

            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(CreateChatData), "Erro ao tentar criar dados de chat para o usuário.", ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteChatData(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (!HasUserConversation(user) && !HasUserConversationModel(user))
                return false;

            if (Information.GuildChatData is not { } gcd)
            {
                LogUtil.Error(nameof(DeleteChatData), "GuildChatData está nulo. Não foi possível deletar os dados de chat.");
                return false;
            }

            gcd.ConversationHistorics.Remove(user.Id);
            gcd.ConversationModels.Remove(user.Id);

            try
            {
                return await this.SaveGuildChatData(gcd);
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(DeleteChatData), "Erro ao salvar as alterações após deletar os dados de chat.", ex.Message);
                return false;
            }
        }

        public async Task<bool> AddCompletionData(IUser user, List<ChatCompletion> completions)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (completions == null || completions.Count == 0)
                throw new ArgumentException("A lista de conclusões não pode ser nula ou vazia.", nameof(completions));

            if (Information.GuildChatData is not { } gcd)
            {
                LogUtil.Error(nameof(AddCompletionData), "GuildChatData está nulo. Não foi possível adicionar os dados de conclusão.");
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
                LogUtil.Error(nameof(AddCompletionData), "Erro ao salvar GuildChatData após adicionar os dados de conclusão.", ex.Message);
                return false;
            }
        }

        public async Task<bool> AddCompletion(IUser user, ChatCompletion completion)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (completion == null) throw new ArgumentNullException(nameof(completion));

            var completionHistorics = CompletionHistorics();
            if (completionHistorics == null)
            {
                LogUtil.Error(nameof(AddCompletion), "CompletionHistorics está nulo. Não foi possível adicionar a conclusão.");
                return false;
            }

            if (!completionHistorics.TryGetValue(user.Id, out var completions))
            {
                LogUtil.Log(nameof(AddCompletion), $"Nenhum histórico encontrado para o usuário {user.Id}. Criando um novo.");

                completions = new List<ChatCompletion>();
                completionHistorics[user.Id] = completions;
            }

            completions.Add(completion);
            return await AddCompletionData(user, completions);
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

        public async Task<bool> UpdateConversationAsync(IUser user, ChatMessage message)
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

        public bool HasUserConversationModel(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return ConversationModels().ContainsKey(user.Id);
        }

        public int GetTotalMessagesByRoleChat(IUser user, ChatMessageRole role)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (ConversationHistorics()?.TryGetValue(user.Id, out var messages) == true)
            {
                //return messages.Count(m => m.Role == role);
            }

            return 0;
        }
    }
}