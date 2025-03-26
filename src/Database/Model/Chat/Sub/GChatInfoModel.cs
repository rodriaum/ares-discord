using Ares.Objects.Chat.Image;

namespace Ares.Database.Model.Chat.Sub;

public class GChatInfoModel
{
    public string Id { get; set; }
    public bool Active { get; set; }
    public ulong Channel { get; set; }
    public string Model { get; set; }
    public ImageGenOptions? ImageGenOptions { get; set; }
    public List<GChatHistoricModel> Historics { get; set; }

    public GChatInfoModel(ulong channel, string model, bool active = false, ImageGenOptions? imageGenOptions = null, List<GChatHistoricModel>? historics = null)
    {
        Id = Guid.NewGuid().ToString();
        Channel = channel;
        Model = model;
        Active = active;
        ImageGenOptions = imageGenOptions;
        Historics = historics ?? new List<GChatHistoricModel>();
    }
}