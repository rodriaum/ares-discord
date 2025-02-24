namespace Ares.src.Guild.Chat.Sub;

public class ChatInfo
{
    public string Id { get; set; }
    public bool Active { get; set; }
    public ulong Channel { get; set; }
    public string Model { get; set; }
    public List<ChatHistoric> Historics { get; set; }

    public ChatInfo(ulong channel, string model, bool active = false, List<ChatHistoric>? historics = null)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Channel = channel;
        this.Model = model;
        this.Active = active;
        this.Historics = (historics != null ? historics : new List<ChatHistoric>());
    }
}