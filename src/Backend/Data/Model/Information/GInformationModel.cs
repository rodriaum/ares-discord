using Ares.src.Backend.Data.Model.Chat;
using Ares.src.Backend.Data.Model.Config;
using Ares.src.Backend.Data.Model.Token;

namespace Ares.src.Backend.Data.Model.Information;

public class GInformationModel
{
    public GTokenModel Token { get; set; }
    public GuildConfigData Config { get; set; }
    public GChatModel Chat { get; set; }



    /// <summary>
    /// Construtor padrão que inicializa a classe com valores padrão.
    /// </summary>
    public GInformationModel()
    {
        Token = new GTokenModel();
        Config = new GuildConfigData();
        Chat = new GChatModel();
    }
}