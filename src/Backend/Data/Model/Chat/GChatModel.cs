using Ares.src.Backend.Data.Model.Chat.Sub;

namespace Ares.src.Backend.Data.Model.Chat;

public class GChatModel
{
    public Dictionary<ulong, List<ChatInfoModel>> Infos { get; set; }

    public GChatModel()
    {
        Infos = new Dictionary<ulong, List<ChatInfoModel>>();
    }
}