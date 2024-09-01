using MongoDB.Bson.IO;
using System.Runtime.CompilerServices;

namespace Discord_OpenAI.Data
{
    internal class Guild
    {
        public readonly string Id;

        private string OpenAiToken { get; set; }

        private string MemberRoleId { get; set; }
        private string OpenAiRoleId { get; set; }
        private string OpenAiExclusiveRoleId { get; set; }

        private string SetupChatsChannelId { get; set; }

        private string ChatsCategoryId { get; set; }

        public Guild(string guildId)
        {
            this.Id = guildId;

            this.OpenAiToken = "";

            this.MemberRoleId = "";
            this.OpenAiRoleId = "";
            this.OpenAiExclusiveRoleId = "";

            this.SetupChatsChannelId = "";

            this.ChatsCategoryId = "";
        }

        /// <summary>
        /// Save account fields.
        /// </summary>
        /// <param name="fields">Fields to save</param>

        public async Task Save(List<string> fields)
        {
            foreach (string field in fields)
            {
                await Core.GuildData.Update(this, field.ToLower());
            }
        }

        public async Task Save(string field)
        {
            await Save(new List<string> { field });
        }

        /** Setup Chats Channel Id */

        public async void SetField(
            string? openAiToken = null,
            string? memberRoleId = null,
            string? openAiRoleId = null,
            string? openAiExclusiveRoleId = null,
            string? setupChatsChannelId = null,
            string? chatsCategoryId = null
            )
        {
            if (openAiToken != null)
            {
                this.OpenAiToken = openAiToken;
                await Save("openAiToken");
            }

            if (memberRoleId != null)
            {
                this.MemberRoleId = memberRoleId;
                await Save("memberRoleId");
            }

            if (openAiRoleId != null)
            {
                this.OpenAiRoleId = openAiRoleId;
                await Save("openAiRoleId");
            }

            if (openAiExclusiveRoleId != null)
            {
                this.OpenAiExclusiveRoleId = openAiExclusiveRoleId;
                await Save("openAiExclusiveRoleId");
            }

            if (setupChatsChannelId != null)
            {
                this.SetupChatsChannelId = setupChatsChannelId;
                await Save("setupChatsChannelId");
            }

            if (chatsCategoryId != null)
            {
                this.ChatsCategoryId = chatsCategoryId;
                await Save("chatsCategoryId");
            }
        }
    }
}
