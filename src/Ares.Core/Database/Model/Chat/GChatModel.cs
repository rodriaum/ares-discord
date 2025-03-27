using Ares.Core.Database.Model.Chat.Sub;

namespace Ares.Core.Database.Model.Chat;

public class GChatModel
{
    public Dictionary<ulong, List<GChatInfoModel>> Infos { get; set; }

    public GChatModel()
    {
        Infos = new Dictionary<ulong, List<GChatInfoModel>>();
    }
}