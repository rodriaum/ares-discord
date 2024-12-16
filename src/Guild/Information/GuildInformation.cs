using Ares.src.Guild.ChatData;
using Ares.src.Objects.OpenAI.Model;
using OpenAI.Chat;
using OpenAI.Images;

namespace Ares.src.Guild.Information
{
    public class GuildInformation
    {
        public string OpenAiToken;

        public GuildIdData? GuildIdData { get; set; }
        public GuildChatData? GuildChatData { get; set; }

        /// <summary>
        /// Construtor padrão que inicializa a classe GuildInformation com valores padrão.
        /// </summary>
        public GuildInformation()
        {
            this.OpenAiToken = "";

            this.GuildIdData = new GuildIdData
            {
                MemberRoleId = 0L,
                UsageRoleId = 0L,
                ExclusiveRoleId = 0L,
                SetupChannelId = 0L,
                ChatsCategoryId = 0L
            };

            this.GuildChatData = new GuildChatData
            {
                ConversationModels = new Dictionary<ulong, OpenAiModel>(),
                ConversationHistorics = new Dictionary<ulong, List<ChatMessage>>(),
                CompletionHistorics = new Dictionary<ulong, List<ChatCompletion>>(),
                GeneratedImageHistorics = new Dictionary<ulong, List<GeneratedImage>>(),
            };
        }

        /// <summary>
        /// Construtor que permite definir valores personalizados para o token da OpenAI e os dados da guilda.
        /// </summary>
        /// <param name="openAiToken">Token da API OpenAI.</param>
        /// <param name="guildIdData">Dados relacionados aos IDs de guilda.</param>
        /// <param name="guildChatData">Dados relacionados às conversas de guilda.</param>
        public GuildInformation(string openAiToken = "", GuildIdData? guildIdData = null, GuildChatData? guildChatData = null)
        {
            this.OpenAiToken = openAiToken;

            this.GuildIdData = guildIdData;
            this.GuildChatData = guildChatData;
        }
    }
}