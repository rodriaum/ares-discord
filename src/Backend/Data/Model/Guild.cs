using Ares.src.Backend.Data.Model.Chat;
using Ares.src.Backend.Data.Model.Chat.Sub;
using Ares.src.Backend.Data.Model.Config;
using Ares.src.Backend.Data.Model.Information;
using Ares.src.Backend.Data.Model.Token;
using Ares.src.Objects.Language;
using Ares.src.Objects.Model;
using Ares.src.Utils.Extra;
using Discord;

namespace Ares.src.Backend.Data.Model;

public class Guild
{
    public readonly string Id;

    public GInformationModel Information;

    /// <summary>
    /// Construtor da classe Guild.
    /// </summary>
    /// <param name="id">Identificador da guilda.</param>
    public Guild(string id)
    {
        Id = id;
        Information = new GInformationModel();
    }

    /// <summary>
    /// Salva os campos especificados na guilda.
    /// </summary>
    /// <param name="fields">Lista de campos a serem salvos.</param>
    /// <returns>Retorna true se os campos foram salvos com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveAsync(List<string> fields)
    {
        if (fields == null || fields.Count == 0)
            throw new ArgumentException("A lista de campos não pode ser nula ou vazia.", nameof(fields));

        if (Core.GuildRepository is not { } guildData)
        {
            LogUtil.Error(nameof(SaveAsync), "GuildData está nulo. Não foi possível salvar os campos.");
            return false;
        }

        try
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                    throw new ArgumentException("A lista de campos contém um valor nulo ou vazio.", nameof(fields));

                await guildData.Update(this, field);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogUtil.Error(nameof(SaveAsync), "Erro ao atualizar um ou vários campos no banco de dados.", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Salva um único campo especificado na guilda.
    /// </summary>
    /// <param name="field">Campo a ser salvo.</param>
    /// <returns>Retorna true se o campo foi salvo com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveAsync(string field)
    {
        return await SaveAsync(new List<string> { field });
    }

    /// <summary>
    /// Salva as informações gerais da guilda.
    /// </summary>
    /// <param name="information">Objeto com as informações da guilda.</param>
    /// <returns>Retorna true se as informações foram salvas com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveInformation(GInformationModel information)
    {
        if (information == null)
        {
            LogUtil.Error("InformationNull", "Não foi possível pegar as informações da guilda. (SaveInformation)");
            return false;
        }

        Information = information;

        return await SaveAsync("Information");
    }

    /// <summary>
    /// Atualiza os dados de chat da guilda no banco de dados.
    /// </summary>
    /// <param name="data">Objeto contendo os dados de chat da guilda.</param>
    /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveChatDataAsync(GChatModel chatData)
    {
        Information.Chat = chatData;
        return await SaveInformation(Information);
    }

    /// <summary>
    /// Adiciona novas informações no banco de dados.
    /// </summary>
    /// <param name="user">Usuário a atualizar no banco de dados.</param>
    /// <param name="infos">Lista de informações a serem adicionadas.</param>
    /// <param name="onlyCached">Opcional: Caso precise ser guardado localmente em vez de no banco de dados.</param>
    public async Task<bool> SaveInfoAsync(IUser user, List<ChatInfoModel> infos, bool onlyCached = false)
    {
        Information.Chat.Infos[user.Id] = infos;
        return !onlyCached ? await SaveInformation(Information) : true;
    }

    public async Task<bool> SaveHistoricAsync(IUser user, ChatInfoModel info)
    {
        List<ChatInfoModel>? list = ChatInfos(user);

        if (list == null) return await Task.FromResult(false);

        list.Add(info);

        await SaveInfoAsync(user, list);
        return await SaveInformation(Information);
    }

    public async Task<bool> UpdateChatInfoAsync(IUser user, ChatInfoModel info)
    {
        List<ChatInfoModel>? infos = ChatInfos(user);

        if (infos == null)
        {
            infos = new List<ChatInfoModel>();
            await SaveInfoAsync(user, infos, onlyCached: true);
        }

        var existingInfo = infos.LastOrDefault(it => it.Channel.Equals(info.Channel));

        if (existingInfo != null)
        {
            Information.Chat.Infos[user.Id].Remove(existingInfo);
        }

        Information.Chat.Infos[user.Id].Add(info);
        return await SaveInformation(Information);
    }

    /// <summary>
    /// Atualiza os dados de ID da guilda no banco de dados.
    /// </summary>
    /// <param name="configData">Objeto contendo os dados de ID da guilda.</param>
    /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveGuildConfigDataAsync(GuildConfigData configData)
    {
        Information.Config = configData;

        return await SaveInformation(Information);
    }

    /// <summary>
    /// Atualiza os dados dos Tokens da guilda no banco de dados.
    /// </summary>
    /// <param name="tokenData">Objeto contendo os dados dos tokens da guilda.</param>
    /// <returns>Retorna true se os dados foram atualizados com sucesso, false caso contrário.</returns>
    public async Task<bool> SaveGuildTokenDataAsync(GTokenModel tokenData)
    {
        Information.Token = tokenData;

        return await SaveInformation(Information);
    }

    /** Sistema de Conversa */

    /// <summary>
    /// Retorna o histórico de conversas da guilda.
    /// </summary>
    /// <returns>Dicionário contendo os históricos de conversas ou null caso não existam.</returns>
    public Dictionary<ulong, List<ChatInfoModel>>? Infos()
    {
        return Information.Chat.Infos;
    }

    public List<ChatHistoricModel>? ChatHistorics(IUser user, ulong channel = 0)
    {
        List<ChatInfoModel>? infos = Infos()?[user.Id];

        if (infos == null) return null;

        if (channel != 0)
        {
            infos = infos.FindAll(historic => historic.Channel == channel);
        }

        List<ChatHistoricModel> historics = infos.SelectMany(info => info.Historics).ToList();

        return historics;
    }

    public List<ChatInfoModel>? ChatInfos(IUser user)
    {
        return Infos()?[user.Id];
    }

    public List<ChatHistoricModel>? ChatHistoricsByChannel(IUser user, ulong channel)
    {
        List<ChatHistoricModel>? historics = ChatHistorics(user, channel: channel);
        if (historics == null) return null;

        return historics;
    }

    public ChatInfoModel? ChatInfoByChannel(IUser user, ulong channel)
    {
        return Infos()?[user.Id].FindLast(historic => historic.Channel == channel);
    }

    public Task<bool> ToggleChatInfo(IUser user, ulong channel, bool active)
    {
        var infos = ChatInfos(user);
        if (infos == null)
        {
            LogUtil.Error(nameof(ToggleChatInfo), "Não foi possível alterar o status de uma informação de um chat.");
            return Task.FromResult(false);
        }

        ChatInfoModel? info = infos.LastOrDefault(i => i.Channel == channel);

        if (info != null)
        {
            info.Active = active;
        }

        return SaveInfoAsync(user, infos);
    }

    public ChatHistoricModel? LastChatHistoric(IUser user, ulong channel = 0)
    {
        if (channel != 0)
        {
            return ChatInfoByChannel(user, channel)?.Historics.LastOrDefault();
        }

        return ChatHistorics(user)?.LastOrDefault();
    }

    public ChatInfoModel? LastChatInfo(IUser user, ulong channel = 0)
    {
        List<ChatInfoModel>? infos = ChatInfos(user);

        if (infos == null || infos.Count == 0)
            return null;

        return channel != 0 ? infos.FindAll(it => it.Channel == channel).LastOrDefault() : infos.LastOrDefault();
    }

    public async Task<bool> CreateChatData(IUser user, ChatInfoModel info)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        if (HasActiveUserConversation(user))
        {
            LogUtil.Log(nameof(CreateChatData), "O usuário já possui uma conversa ou modelo. Nenhuma ação necessária.");
            return await Task.FromResult(false);
        }

        GChatModel chat = Information.Chat;

        if (chat == null)
        {
            LogUtil.Error(nameof(CreateChatData), "GuildChatData está nulo. Não foi possível criar dados de chat para o usuário.");
            return await Task.FromResult(false);
        }

        try
        {
            if (!chat.Infos.TryGetValue(user.Id, out var infos))
            {
                infos = new List<ChatInfoModel>();
                chat.Infos[user.Id] = infos;
            }

            if (info.Historics == null)
            {
                info.Historics = new List<ChatHistoricModel>();
            }

            infos.Add(info);

            bool success = await SaveChatDataAsync(chat);

            if (success)
            {
                LogUtil.Log("Chat", $"Chat ID \"{info.Id}\" successfully created by \"{user.Username}#{user.Discriminator}\"");
            }

            return success;
        }
        catch (Exception ex)
        {
            LogUtil.Error(nameof(CreateChatData), "Erro ao tentar criar um histórico de chat para o usuário.", ex.Message);
            return await Task.FromResult(false);
        }
    }

    public async Task<bool> UpdateChatHistoricsAsync(IUser user, ulong channel, List<ChatHistoricModel> historics)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (historics == null) throw new ArgumentNullException(nameof(historics));

        if (Information.Chat is not { } chat)
            return false;

        ChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        info.Historics = historics;

        Information.Chat = chat;
        return await SaveInformation(Information);
    }

    public async Task<bool> UpdateChatHistoricsAsync(IUser user, ulong channel, ChatHistoricModel historic)
    {
        ChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        List<ChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), "Conversation historics are null.");
            return false;
        }

        historics.Add(historic);

        return await UpdateChatHistoricsAsync(user, channel, historics);
    }

    public async Task<bool> RemoveConversationAsync(IUser user, ulong channel, ChatHistoricModel historic)
    {
        ChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        List<ChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            LogUtil.Error(nameof(this.RemoveConversationAsync), "Conversation historics are null.");
            return false;
        }

        historics.Remove(historic);

        return await UpdateChatHistoricsAsync(user, channel, historics);
    }

    /// <summary>
    /// Verifica se o usuário possui uma conversa existente.
    /// </summary>
    /// <param name="user">Usuário alvo.</param>
    /// <returns>Retorna true se a conversa existe, caso contrário, false.</returns>
    public bool HasActiveUserConversation(IUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var infos = Infos();

        if (infos == null)
        {
            LogUtil.Error(nameof(HasActiveUserConversation), "Não foi possível obter o histórico de informações.");
            return false;
        }

        var info = infos.TryGetValue(user.Id, out var value);

        return value != null && value.Count > 0 && value[value.Count - 1].Active;
    }

    public ChatModel? GetLastModelByUser(IUser user, ulong channel = 0)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var info = LastChatInfo(user, channel: channel);
        if (info == null) return null;

        var model = info.Model;
        if (string.IsNullOrWhiteSpace(model)) return null;

        return ChatModel.GetByNearestModel(model);
    }

    /// <summary>
    /// Languages
    /// </summary>
    public string Language()
    {
        return Information.Config.Lang;
    }

    public LangCategory LanguageCategory()
    {
        return Core.LangManager.GetCategoryByCode(Language());
    }

    public string GetTranslation(string code)
    {
        return Core.LangManager.GetTranslation(LanguageCategory(), code);
    }
}