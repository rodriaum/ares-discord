namespace Ares.src.Guild.Chat.Sub;

public class ChatInfo
{
    public bool Active { get; set; }
    public ulong Channel { get; set; }

    public ChatInfo(ulong channel, bool active = false)
    {
        this.Channel = channel;
        this.Active = active;
    }
}