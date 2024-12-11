using Ares.src.Guild.ChatData;
using Ares.src.Guild.Information;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Util.Extra;
using Discord;
using OpenAI.Chat;

namespace Ares.src.Guild
{
    internal class Guild
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

        /** Conversation System */

        public Dictionary<ulong, List<ChatMessage>>? ConversationHistorics()
        {
            GuildChatData? guildChatData = Information.GuildChatData;

            return (guildChatData != null ? guildChatData.ConversationHistorics : null);
        }

        public Dictionary<ulong, List<ChatCompletion>>? CompletionHistorics()
        {
            GuildChatData? guildChatData = Information.GuildChatData;

            return (guildChatData != null ? guildChatData.CompletionHistorics : null);
        }

        public List<ChatMessage>? Messages(IUser user)
        {
            if (CompletionHistorics() == null) return null;

            return ConversationHistorics()?.GetValueOrDefault(user.Id);
        }

        public async Task<bool> CreateChatData(IUser user, OpenAiModel model)
        {
            if (HasUserConversation(user) || HasUserConversationModel(user))
                return false;

            GuildChatData? guildChatData = Information.GuildChatData;

            bool sucess =
                guildChatData != null &&
                guildChatData.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                guildChatData.ConversationModels.TryAdd(user.Id, model);

            GuildInformation updatedInformation = Information;
            updatedInformation.GuildChatData = guildChatData;

            await SaveInformation(updatedInformation);

            return sucess;
        }

        public async Task DeleteChatData(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (!HasUserConversation(user) && !HasUserConversationModel(user))
                return;

            if (Information.GuildChatData is not { } gcd)
            {
                LogUtil.Error(nameof(DeleteChatData), "GuildChatData está nulo. Não foi possível deletar os dados de chat.");
                return;
            }

            gcd.ConversationHistorics.Remove(user.Id);
            gcd.ConversationModels.Remove(user.Id);

            try
            {
                GuildInformation updatedInformation = Information;
                updatedInformation.GuildChatData = gcd;

                await SaveInformation(updatedInformation);
            }
            catch (Exception ex)
            {
                LogUtil.Error(nameof(DeleteChatData), "Erro ao salvar as alterações após deletar os dados de chat.", ex.Message);
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

        public async Task<bool> AddConversationAsync(IUser user, List<ChatMessage> messages)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            if (Information.GuildChatData is not { } guildChatData)
                return false;

            guildChatData.ConversationHistorics[user.Id] = messages;

            Information.GuildChatData = guildChatData;
            return await SaveInformation(Information);
        }

        public async Task<bool> AddConversationAsync(IUser user, ChatMessage message)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var conversationHistorics = this.ConversationHistorics();

            if (conversationHistorics == null)
            {
                LogUtil.Error(nameof(this.AddConversationAsync), "Conversation historics are null.");
                return false;
            }

            if (!conversationHistorics.TryGetValue(user.Id, out var messages) || messages == null)
            {
                LogUtil.Error(nameof(this.AddConversationAsync), $"Cannot retrieve chat messages for user ID {user.Id}.");
                return false;
            }

            messages.Add(message);

            return await this.AddConversationAsync(user, messages);
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