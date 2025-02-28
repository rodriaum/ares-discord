namespace Ares.src.Backend.Data.Model.Chat.Sub;

public class ChatInfoModel
{
    public string Id { get; set; }
    public bool Active { get; set; }
    public ulong Channel { get; set; }
    public string Model { get; set; }
    public List<ChatHistoricModel> Historics { get; set; }

    public ChatInfoModel(ulong channel, string model, bool active = false, List<ChatHistoricModel>? historics = null)
    {
        Id = Guid.NewGuid().ToString();
        Channel = channel;
        Model = model;
        Active = active;
        Historics = historics != null ? historics : new List<ChatHistoricModel>();
    }
}