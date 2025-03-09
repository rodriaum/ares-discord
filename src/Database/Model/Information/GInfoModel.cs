using Ares.src.Database.Model.Chat;
using Ares.src.Database.Model.Config;
using Ares.src.Database.Model.Token;

namespace Ares.src.Database.Model.Information;

public class GInfoModel
{
    public GTokenModel Token { get; set; }
    public GuildConfigData Config { get; set; }
    public GChatModel Chat { get; set; }

    public GInfoModel()
    {
        Token = new GTokenModel();
        Config = new GuildConfigData();
        Chat = new GChatModel();
    }
}