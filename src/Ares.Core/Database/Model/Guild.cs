using Ares.Core.Database.Model.Chat;
using Ares.Core.Database.Model.Chat.Sub;
using Ares.Core.Database.Model.Config;
using Ares.Core.Database.Model.Information;
using Ares.Core.Database.Model.Token;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using Ares.Discord;
using Discord;

namespace Ares.Core.Database.Model;

/// <summary>
/// Represents a Discord guild (server) with its associated data and operations.
/// </summary>
public class Guild
{
    /// <summary>
    /// The unique identifier of the guild.
    /// </summary>
    public readonly string Id;

    /// <summary>
    /// Contains all information related to the guild.
    /// </summary>
    public GInfoModel Information;

    /// <summary>
    /// Initializes a new instance of the Guild class.
    /// </summary>
    /// <param name="id">The identifier of the guild.</param>
    public Guild(string id)
    {
        this.Id = id;
        this.Information = new GInfoModel();
    }

    /// <summary>
    /// Saves the specified fields of the guild to the database.
    /// </summary>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public async Task<bool> SaveAsync(List<string> fields)
    {
        if (fields == null || fields.Count == 0)
        {
            AresLogger.Error(nameof(SaveAsync), "The field list is null or empty.");
            return false;
        }

        if (AresCore.GuildCollection is not { } guildData)
        {
            AresLogger.Error(nameof(SaveAsync), "Guild data is null. Unable to save fields.");
            return false;
        }

        try
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    AresLogger.Error(nameof(SaveAsync), "The field list contains a null or empty value.");
                    continue;
                }

                await guildData.UpdateAsync(this, field);
            }

            return true;
        }
        catch (Exception ex)
        {
            AresLogger.Error(nameof(SaveAsync), "Error updating one or more fields in the database.", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Saves a single field of the guild to the database.
    /// </summary>
    /// <param name="field">The field name to be saved.</param>
    /// <returns>Returns true if the field was successfully saved, false otherwise.</returns>
    public async Task<bool> SaveAsync(string field)
    {
        return await SaveAsync(new List<string> { field });
    }

    /// <summary>
    /// Saves general information about the guild to the database.
    /// </summary>
    /// <param name="information">Object containing guild information.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public async Task<bool> SaveInformation(GInfoModel information)
    {
        if (information == null)
        {
            AresLogger.Error(nameof(SaveInformation), "Unable to get guild information.");
            return false;
        }

        Information = information;

        return await SaveAsync("Information");
    }

    /// <summary>
    /// Updates the chat data of the guild in the database.
    /// </summary>
    /// <param name="chatData">Object containing the guild's chat data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public async Task<bool> SaveChatDataAsync(GChatModel chatData)
    {
        Information.Chat = chatData;
        return await SaveInformation(Information);
    }

    /// <summary>
    /// Adds new information to the database for a specific user.
    /// </summary>
    /// <param name="user">User to update in the database.</param>
    /// <param name="infos">List of information to be added.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public async Task<bool> SaveInfoAsync(IUser user, List<GChatInfoModel> infos, bool onlyCached = false)
    {
        Information.Chat.Infos[user.Id] = infos;
        return !onlyCached ? await SaveInformation(Information) : true;
    }

    /// <summary>
    /// Saves chat history information for a specific user.
    /// </summary>
    /// <param name="user">The user to save history for.</param>
    /// <param name="info">Chat information to be saved.</param>
    /// <returns>Returns true if the history was successfully saved, false otherwise.</returns>
    public async Task<bool> SaveHistoricAsync(IUser user, GChatInfoModel info)
    {
        List<GChatInfoModel>? list = ChatInfos(user);

        if (list == null) return await Task.FromResult(false);

        list.Add(info);

        await SaveInfoAsync(user, list);
        return await SaveInformation(Information);
    }

    /// <summary>
    /// Updates chat information for a specific user and channel.
    /// </summary>
    /// <param name="user">The user whose information should be updated.</param>
    /// <param name="info">The updated chat information.</param>
    /// <returns>Returns true if information was successfully updated, false otherwise.</returns>
    public async Task<bool> UpdateChatInfoAsync(IUser user, GChatInfoModel info)
    {
        List<GChatInfoModel>? infos = ChatInfos(user);

        if (infos == null)
        {
            infos = new List<GChatInfoModel>();
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
    /// Updates the guild configuration data in the database.
    /// </summary>
    /// <param name="configData">Object containing the guild configuration data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public async Task<bool> SaveGuildConfigDataAsync(GuildConfigData configData)
    {
        Information.Config = configData;

        return await SaveInformation(Information);
    }

    /// <summary>
    /// Updates the guild token data in the database.
    /// </summary>
    /// <param name="tokenData">Object containing the guild token data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public async Task<bool> SaveGuildTokenDataAsync(GTokenModel tokenData)
    {
        Information.Token = tokenData;

        return await SaveInformation(Information);
    }

    /** Conversation System **/

    /// <summary>
    /// Retrieves the conversation history of the guild.
    /// </summary>
    /// <returns>Dictionary containing conversation histories or null if they don't exist.</returns>
    public Dictionary<ulong, List<GChatInfoModel>>? Infos()
    {
        return Information.Chat.Infos;
    }

    /// <summary>
    /// Retrieves chat history records for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The user to get chat history for.</param>
    /// <param name="channel">Optional: Channel ID to filter history by. If 0, returns all channels.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public List<GChatHistoricModel>? ChatHistorics(IUser user, ulong channel = 0)
    {
        List<GChatInfoModel>? infos = Infos()?[user.Id];

        if (infos == null) return null;

        if (channel != 0)
        {
            infos = infos.FindAll(historic => historic.Channel == channel);
        }

        List<GChatHistoricModel> historics = infos.SelectMany(info => info.Historics).ToList();

        return historics;
    }

    /// <summary>
    /// Retrieves all chat information for a specific user.
    /// </summary>
    /// <param name="user">The user to get chat information for.</param>
    /// <returns>List of chat information objects or null if not found.</returns>
    public List<GChatInfoModel>? ChatInfos(IUser user)
    {
        return Infos()?[user.Id];
    }

    /// <summary>
    /// Retrieves chat history records for a specific user in a specific channel.
    /// </summary>
    /// <param name="user">The user to get chat history for.</param>
    /// <param name="channel">The channel ID to filter history by.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public List<GChatHistoricModel>? ChatHistoricsByChannel(IUser user, ulong channel)
    {
        List<GChatHistoricModel>? historics = ChatHistorics(user, channel: channel);
        if (historics == null) return null;

        return historics;
    }

    /// <summary>
    /// Retrieves the latest chat information for a specific user in a specific channel.
    /// </summary>
    /// <param name="user">The user to get chat information for.</param>
    /// <param name="channel">The channel ID to filter information by.</param>
    /// <returns>Chat information object or null if not found.</returns>
    public GChatInfoModel? ChatInfoByChannel(IUser user, ulong channel)
    {
        return Infos()?[user.Id].FindLast(historic => historic.Channel == channel);
    }

    /// <summary>
    /// Toggles the active status of a chat for a specific user and channel.
    /// </summary>
    /// <param name="user">The user whose chat status should be toggled.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="active">The new active status: true to activate, false to deactivate.</param>
    /// <returns>Returns true if status was successfully changed, false otherwise.</returns>
    public Task<bool> ToggleChatInfo(IUser user, ulong channel, bool active)
    {
        var infos = ChatInfos(user);
        if (infos == null)
        {
            AresLogger.Error(nameof(ToggleChatInfo), "Unable to change the status of a chat information.");
            return Task.FromResult(false);
        }

        GChatInfoModel? info = infos.LastOrDefault(i => i.Channel == channel);

        if (info != null)
        {
            info.Active = active;
        }

        return SaveInfoAsync(user, infos);
    }

    /// <summary>
    /// Retrieves the last chat history record for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The user to get the last chat history for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last record from any channel.</param>
    /// <returns>The last chat history record or null if not found.</returns>
    public GChatHistoricModel? LastChatHistoric(IUser user, ulong channel = 0)
    {
        if (channel != 0)
        {
            return ChatInfoByChannel(user, channel)?.Historics.LastOrDefault();
        }

        return ChatHistorics(user)?.LastOrDefault();
    }

    /// <summary>
    /// Retrieves the last chat information for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The user to get the last chat information for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last information from any channel.</param>
    /// <returns>The last chat information or null if not found.</returns>
    public GChatInfoModel? LastChatInfo(IUser user, ulong channel = 0)
    {
        List<GChatInfoModel>? infos = ChatInfos(user);

        if (infos == null || infos.Count == 0)
            return null;

        return channel != 0 ? infos.FindAll(it => it.Channel == channel).LastOrDefault() : infos.LastOrDefault();
    }

    /// <summary>
    /// Creates new chat data for a user.
    /// </summary>
    /// <param name="user">The user to create chat data for.</param>
    /// <param name="info">The chat information to be created.</param>
    /// <returns>Returns true if chat data was successfully created, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public async Task<bool> CreateChatData(IUser user, GChatInfoModel info)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        if (HasActiveUserConversation(user))
        {
            AresLogger.Log(nameof(CreateChatData), "User already has a conversation or template. No action required.");
            return await Task.FromResult(false);
        }

        GChatModel chat = Information.Chat;

        if (chat == null)
        {
            AresLogger.Error(nameof(CreateChatData), "Guild chat data is null. Unable to create chat data for the user.");
            return await Task.FromResult(false);
        }

        try
        {
            if (!chat.Infos.TryGetValue(user.Id, out var infos))
            {
                infos = new List<GChatInfoModel>();
                chat.Infos[user.Id] = infos;
            }

            if (info.Historics == null)
            {
                info.Historics = new List<GChatHistoricModel>();
            }

            infos.Add(info);

            bool success = await SaveChatDataAsync(chat);

            if (success)
            {
                AresLogger.Log("Chat", $"Chat ID \"{info.Id}\" successfully created by \"{user.Username}#{user.Discriminator}\"");
            }

            return success;
        }
        catch (Exception ex)
        {
            AresLogger.Error(nameof(CreateChatData), "Error trying to create a chat history for the user.", ex.Message);
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// Updates chat history records for a specific user and channel.
    /// </summary>
    /// <param name="user">The user whose chat history should be updated.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="historics">The updated list of chat history records.</param>
    /// <returns>Returns true if history was successfully updated, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user or historics is null.</exception>
    public async Task<bool> UpdateChatHistoricsAsync(IUser user, ulong channel, List<GChatHistoricModel> historics)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (historics == null) throw new ArgumentNullException(nameof(historics));

        if (Information.Chat is not { } chat)
            return false;

        GChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        info.Historics = historics;

        Information.Chat = chat;
        return await SaveInformation(Information);
    }

    /// <summary>
    /// Adds a single chat history record for a specific user and channel.
    /// </summary>
    /// <param name="user">The user to add chat history for.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="historic">The chat history record to add.</param>
    /// <returns>Returns true if history was successfully added, false otherwise.</returns>
    public async Task<bool> UpdateChatHistoricsAsync(IUser user, ulong channel, GChatHistoricModel historic)
    {
        GChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Error(nameof(this.UpdateChatHistoricsAsync), "Conversation historics are null.");
            return false;
        }

        historics.Add(historic);

        return await UpdateChatHistoricsAsync(user, channel, historics);
    }

    /// <summary>
    /// Removes a specific conversation history record for a user and channel.
    /// </summary>
    /// <param name="user">The user whose conversation record should be removed.</param>
    /// <param name="channel">The channel ID for the conversation.</param>
    /// <param name="historic">The specific history record to remove.</param>
    /// <returns>Returns true if the record was successfully removed, false otherwise.</returns>
    public async Task<bool> RemoveConversationAsync(IUser user, ulong channel, GChatHistoricModel historic)
    {
        GChatInfoModel? info = ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(this.UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {user.Id} and channel {channel}.");
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Error(nameof(this.RemoveConversationAsync), "Conversation historics are null.");
            return false;
        }

        historics.Remove(historic);

        return await UpdateChatHistoricsAsync(user, channel, historics);
    }

    /// <summary>
    /// Checks if a user has an active conversation.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns>Returns true if an active conversation exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public bool HasActiveUserConversation(IUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var infos = Infos();

        if (infos == null)
        {
            AresLogger.Error(nameof(HasActiveUserConversation), "Unable to get historical information.");
            return false;
        }

        var info = infos.TryGetValue(user.Id, out var value);

        return value != null && value.Count > 0 && value[value.Count - 1].Active;
    }

    /// <summary>
    /// Gets the last chat model used by a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The user to get the model for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last model from any channel.</param>
    /// <returns>The last chat model or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
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
    /// Gets the language code configured for this guild.
    /// </summary>
    /// <returns>The language code string.</returns>
    public string Language()
    {
        return Information.Config.Lang;
    }

    /// <summary>
    /// Gets the language category object based on the guild's configured language.
    /// </summary>
    /// <returns>The language category object or null if not found.</returns>
    public LangCategory? LangCategory()
    {
        return AresCore.LangManager.GetCategoryByCode(this.Language());
    }

    /// <summary>
    /// Gets a translated string based on the guild's configured language.
    /// </summary>
    /// <param name="code">The translation code to look up.</param>
    /// <returns>The translated string or the original code if translation was not found.</returns>
    public string GetTranslation(string code)
    {
        LangCategory? category = LangCategory();
        if (category == null) return code;

        return AresCore.LangManager.GetTranslation(category, code);
    }
}