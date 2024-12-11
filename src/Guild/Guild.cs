using Discord;
using OpenAI.Chat;
using Ares.src.Guild.ChatData;
using Ares.src.Util.Extra;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Guild.Information;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public async Task Save(List<string> fields)
        {
            GuildData? data = Core.GuildData;

            if (data == null)
            {
                LogUtil.Error("GuildDataNull", "Não foi possível pegar a data da guilda. (Save)");
                return;
            }

            foreach (string field in fields)
            {
                await data.Update(this, field);
            }
        }

        public async Task Save(string field)
        {
            await Save(new List<string> { field });
        }

        public async Task SaveInformation(GuildInformation information)
        {
            if (information == null)
            {
                LogUtil.Error("InformationNull", "Não foi possível pegar as informações da guilda. (SaveInformation)");
                return;
            }

            this.Information = information;

            await Save("Information");
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

        public async Task<bool> CreateConversation(IUser user, OpenAiModel model)
        {
            if (HasUserConversation(user) || HasUserConversationModel(user))
                return false;

            GuildChatData? guildChatData = Information.GuildChatData;

            bool sucess =
                guildChatData != null &&
                guildChatData.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                guildChatData.ConversationModels.TryAdd(user.Id, model);

            await SaveGuildChatData();

            return sucess;
        }

        public async Task EndConversation(IUser user)
        {
            if (!HasUserConversation(user) || !HasUserConversationModel(user))
                return;

            GuildChatData? guildChatData = Information.GuildChatData;
            if (guildChatData == null) return;  

            guildChatData.ConversationHistorics.Remove(user.Id);
            guildChatData.ConversationModels.Remove(user.Id);

            await SaveGuildChatData();
        }

        public async void AddCompletion(IUser user, List<ChatCompletion> completions)
        {
            GuildChatData? guildChatData = Information.GuildChatData;

            if (guildChatData == null)
            {

            }

            if (HasUserCompletion(user))
                guildChatData.CompletionHistorics[user.Id] = completions;
            else
                guildChatData.CompletionHistorics.Add(user.Id, completions);

            await SaveGuildChatData();
        }

        public bool AddCompletion(IUser user, ChatCompletion completion)
        {
            if (CompletionHistorics() == null) return false;

            CompletionHistorics().TryGetValue(user.Id, out List<ChatCompletion>? completions);

            if (completions == null)
            {
                LogUtil.Error("METHOD", "Cannot get chat completion from user.");
                return false;
            }

            completions.Add(completion);
            AddCompletion(user, completions);

            return true;
        }

        public async void AddConversation(IUser user, List<ChatMessage> messages)
        {
            GuildChatData? guildChatData = Information.GuildChatData;
            if (guildChatData == null) return;

            if (HasUserConversation(user))
                guildChatData.ConversationHistorics[user.Id] = messages;
            else
                guildChatData.ConversationHistorics.Add(user.Id, messages);

            await SaveGuildChatData();
        }

        public bool AddConversation(IUser user, ChatMessage message)
        {
            if (CompletionHistorics() == null) return false;

            ConversationHistorics().TryGetValue(user.Id, out List<ChatMessage>? messages);

            if (messages == null)
            {
                LogUtil.Error("METHOD", "Cannot get chat messages from user.");
                return false;
            }

            messages.Add(message);
            AddConversation(user, messages);

            return true;
        }

        public async Task SaveGuildChatData()
        {
            await Save("GuildChatData");
        }

        public bool HasUserConversation(IUser user)
        {
            if (ConversationHistorics() == null)
            {
                LogUtil.Error("ComplHistoricNull", "Não foi possível pegar o histórico de completações. (HasUserConversation)");
                return false;
            }

            return ConversationHistorics().ContainsKey(user.Id);
        }

        public bool HasUserCompletion(IUser user)
        {
            if (CompletionHistorics() == null)
            {
                LogUtil.Error("ComplHistoricNull", "Não foi possível pegar o histórico de completações. (HasUserConversation)");
                return false;
            }

            return CompletionHistorics().ContainsKey(user.Id);
        }

        public Dictionary<ulong, OpenAiModel> ConversationModels()
        {
            return Information.GuildChatData.ConversationModels;

        }
        public bool AddModel(IUser user, OpenAiModel model)
        {
            GuildChatData? guildChatData = Information.GuildChatData;

            if (guildChatData == null)
            {
                LogUtil.Error("ChatDataNul", "Não foi possível pegar as informações do chat. (AddModel)");
                return false;
            }

            return guildChatData.ConversationModels.TryAdd(user.Id, model);
        }

        public OpenAiModel? GetModelByUser(IUser user)
        {
            ConversationModels().TryGetValue(user.Id, out OpenAiModel? model);
            return model;
        }

        public bool HasUserConversationModel(IUser user)
        {
            return ConversationModels().TryGetValue(user.Id, out _);
        }

        public int GetTotalMessagesByRoleChat(IUser user, ChatMessageRole role)
        {
            return 0;
        }
    }
}