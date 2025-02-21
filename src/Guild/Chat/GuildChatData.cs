using Ares.src.Guild.Chat.Sub;

namespace Ares.src.Guild.Data
{
    public class GuildChatData
    {
        public Dictionary<ulong, List<ChatHistoric>> Historics { get; set; }

        public GuildChatData()
        {
            this.Historics = new Dictionary<ulong, List<ChatHistoric>>();
        }
    }
}