using Discord;
using OpenAI.Chat;
using Ares.src.Guild.ChatData;
using Ares.src.Guild.IdData;
using Ares.src.Util.Extra;
using Ares.src.Backend.Data;
using Ares.src.Objects.OpenAI.Model;

namespace Ares.src.Guild
{
    internal class Guild
    {
        public readonly string Id;

        public string OpenAiToken { get; set; }

        public GuildIdData GuildIdData { get; set; }

        public GuildChatData GuildChatData { get; set; }

        public Guild(string id)
        {
            Id = id;

            OpenAiToken = "";

            GuildIdData = new GuildIdData
            {
                MemberRoleId = 0L,
                UsageRoleId = 0L,
                ExclusiveRoleId = 0L,
                SetupChannelId = 0L,
                ChatsCategoryId = 0L
            };

            GuildChatData = new GuildChatData
            {
                ConversationModels = new Dictionary<ulong, OpenAiModel>(),
                ConversationHistorics = new Dictionary<ulong, List<ChatMessage>>(),
                CompletionHistorics = new Dictionary<ulong, List<ChatCompletion>>()
            };
        }

        /// <summary>
        /// Save account fields.
        /// </summary>
        /// <param name="fields">Fields to save</param>

        public async Task Save(List<string> fields)
        {
            GuildData? data = Core.GuildData;
            if (data == null) return;

            foreach (string field in fields)
            {
                await data.Update(this, field);
            }
        }

        public async Task Save(string field)
        {
            await Save(new List<string> { field });
        }

        /** Setup Chats Channel Id */

        public async void SetField(string? openAiToken = null, GuildIdData? guildIdData = null)
        {
            if (openAiToken != null)
            {
                OpenAiToken = openAiToken;
                await Save("OpenAiToken");
            }

            if (guildIdData != null)
            {
                GuildIdData = guildIdData;
                await Save("GuildIdData");
            }
        }

        /** Conversation System */

        public Dictionary<ulong, List<ChatMessage>> ConversationHistorics()
        {
            return GuildChatData.ConversationHistorics;
        }

        public Dictionary<ulong, List<ChatCompletion>> CompletionHistorics()
        {
            return GuildChatData.CompletionHistorics;
        }

        public List<ChatMessage>? Messages(IUser user)
        {
            return GuildChatData.ConversationHistorics.GetValueOrDefault(user.Id);
        }

        public async Task<bool> CreateConversation(IUser user, OpenAiModel model)
        {
            if (HasUserConversation(user) || HasUserConversationModel(user))
                return false;

            bool sucess =
                GuildChatData.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                GuildChatData.ConversationModels.TryAdd(user.Id, model);

            await SaveGuildChatData();

            return sucess;
        }

        public async Task EndConversation(IUser user)
        {
            if (!HasUserConversation(user) || !HasUserConversationModel(user))
                return;

            GuildChatData.ConversationHistorics.Remove(user.Id);
            GuildChatData.ConversationModels.Remove(user.Id);

            await SaveGuildChatData();
        }

        public async void AddCompletion(IUser user, List<ChatCompletion> completions)
        {
            if (HasUserCompletion(user))
                GuildChatData.CompletionHistorics[user.Id] = completions;
            else
                GuildChatData.CompletionHistorics.Add(user.Id, completions);

            await SaveGuildChatData();
        }

        public void AddCompletion(IUser user, ChatCompletion completion)
        {
            CompletionHistorics().TryGetValue(user.Id, out List<ChatCompletion>? completions);

            if (completions == null)
            {
                LogUtil.Error("METHOD", "Cannot get chat completion from user.");
                return;
            }

            completions.Add(completion);

            AddCompletion(user, completions);
        }

        public async void AddConversation(IUser user, List<ChatMessage> messages)
        {
            if (HasUserConversation(user))
                GuildChatData.ConversationHistorics[user.Id] = messages;
            else
                GuildChatData.ConversationHistorics.Add(user.Id, messages);

            await SaveGuildChatData();
        }

        public void AddConversation(IUser user, ChatMessage message)
        {
            ConversationHistorics().TryGetValue(user.Id, out List<ChatMessage>? messages);

            if (messages == null)
            {
                LogUtil.Error("METHOD", "Cannot get chat messages from user.");
                return;
            }

            messages.Add(message);

            AddConversation(user, messages);
        }

        public async Task SaveGuildChatData()
        {
            await Save("GuildChatData");
        }

        public bool HasUserConversation(IUser user)
        {
            return ConversationHistorics().ContainsKey(user.Id);
        }

        public bool HasUserCompletion(IUser user)
        {
            return CompletionHistorics().ContainsKey(user.Id);
        }

        public Dictionary<ulong, OpenAiModel> ConversationModels()
        {
            return GuildChatData.ConversationModels;

        }
        public bool AddModel(IUser user, OpenAiModel model)
        {
            return GuildChatData.ConversationModels.TryAdd(user.Id, model);
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