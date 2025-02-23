namespace Ares.src.Guild.Chat.Sub;

public class ChatInfo
{
    public bool Active { get; set; }
    public ulong Channel { get; set; }
    public string Model { get; set; }

    public ChatInfo(ulong channel, string model, bool active = false)
    {
        this.Channel = channel;
        this.Model = model;
        this.Active = active;
    }
}