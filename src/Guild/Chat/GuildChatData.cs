using Ares.src.Guild.Chat.Sub;

namespace Ares.src.Guild.Data;

public class GuildChatData
{
    public Dictionary<ulong, List<ChatInfo>> Infos { get; set; }

    public GuildChatData()
    {
        this.Infos = new Dictionary<ulong, List<ChatInfo>>();
    }
}