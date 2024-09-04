using Discord;
using Ares.Backend.Data;
using Ares.Guild.ChatData;
using Ares.Guild.IdData;
using Ares.Objects.OpenAI;
using OpenAI.Chat;

namespace Ares.Guild
{
    internal class Guild
    {
        public readonly string Id;

        public string OpenAiToken { get; set; }

        public GuildIdData GuildIdData { get; set; }

        public GuildChatData GuilChatData { get; set; }

        public Guild(string id)
        {
            this.Id = id;

            this.OpenAiToken = "";

            this.GuildIdData = new GuildIdData
            {
                MemberRoleId = 0L,
                UsageRoleId = 0L,
                ExclusiveRoleId = 0L,
                SetupChannelId = 0L,
                ChatsCategoryId = 0L
            };

            this.GuilChatData = new GuildChatData
            {
                ConversationModels = new Dictionary<ulong, OpenAiModel>(),
                ConversationHistorics = new Dictionary<ulong, List<ChatMessage>>()
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
                this.OpenAiToken = openAiToken;
                await Save("OpenAiToken");
            }

            if (guildIdData != null)
            {
                this.GuildIdData = guildIdData;
                await Save("GuildIdData");
            }
        }

        /** Conversation System */

        public Dictionary<ulong, List<ChatMessage>> ConversationHistorics()
        {
            return this.GuilChatData.ConversationHistorics;
        }

        public async Task<bool> CreateConversation(IUser user, OpenAiModel model)
        {
            if (HasUserConversation(user) || HasUserConversationModel(user))
                return false;

            bool sucess = 
                this.GuilChatData.ConversationHistorics.TryAdd(user.Id, new List<ChatMessage>()) &&
                this.GuilChatData.ConversationModels.TryAdd(user.Id, model);

            await Save("GuilChatData");

            return sucess;
        }

        public async Task EndConversation(IUser user)
        {
            if (!HasUserConversation(user) || !HasUserConversationModel(user))
                return;

            this.GuilChatData.ConversationHistorics.Remove(user.Id);
            this.GuilChatData.ConversationModels.Remove(user.Id);

            await Save("GuilChatData");
        }

        public void AddConversation(IUser user, List<ChatMessage> messages)
        {
            this.GuilChatData.ConversationHistorics.Add(user.Id, messages);
        }

        public bool HasUserConversation(IUser user)
        {
            return ConversationHistorics().ContainsKey(user.Id);
        }

        public Dictionary<ulong, OpenAiModel> ConversationModels()
        {
            return this.GuilChatData.ConversationModels;

        }
        public bool AddModel(IUser user, OpenAiModel model)
        {
            return this.GuilChatData.ConversationModels.TryAdd(user.Id, model);
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
    }
}