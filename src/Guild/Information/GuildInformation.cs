using Ares.src.Guild.Config;
using Ares.src.Guild.Data;
using Ares.src.Guild.Token;

namespace Ares.src.Guild.Information
{
    public class GuildInformation
    {
        public GuildTokenData Token { get; set; }
        public GuildConfigData Config { get; set; }
        public GuildChatData Chat { get; set; }

        /// <summary>
        /// Construtor padrão que inicializa a classe com valores padrão.
        /// </summary>
        public GuildInformation()
        {
            this.Token = new GuildTokenData();
            this.Config = new GuildConfigData();
            this.Chat = new GuildChatData();
        }
    }
}