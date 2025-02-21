using Ares.src.Guild.Config;
using Ares.src.Guild.Data;

namespace Ares.src.Guild.Information
{
    public class GuildInformation
    {
        public string OpenAiToken;

        public GuildConfigData? Config { get; set; }
        public GuildChatData Chat { get; set; }

        /// <summary>
        /// Construtor padrão que inicializa a classe com valores padrão.
        /// </summary>
        public GuildInformation()
        {
            this.OpenAiToken = "";

            this.Config = new GuildConfigData();
            this.Chat = new GuildChatData();
        }
    }
}