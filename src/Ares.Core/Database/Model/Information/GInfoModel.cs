using Ares.Core.Database.Model.Chat;
using Ares.Core.Database.Model.Config;
using Ares.Core.Database.Model.Token;

namespace Ares.Core.Database.Model.Information;

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