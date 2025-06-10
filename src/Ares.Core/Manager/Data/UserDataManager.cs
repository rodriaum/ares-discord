/*
* Copyright (C) Rodrigo Ferreira, All Rights Reserved
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
*/

using Ares.Core.Models.Chat;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Chat.Model;
using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;

namespace Ares.Core.Manager.Data;

/// <summary>
/// User service to manage data and operations.
/// </summary>
public class UserDataManager
{
    /// <summary>
    /// Repository for user data operations.
    /// </summary>
    private static readonly UserRepository? _repository = AppCore.UserRepository;

    public UserDataManager()
    {
        if (_repository is null)
        {
            AresLogger.Log(nameof(UserDataManager), "Repository is not initialized.", severity: Severity.Error);
        }
    }

    /// <summary>
    /// Saves the specified fields of the user to the database.
    /// </summary>
    /// <param name="user">The user to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveAsync(User user, params string[] fields)
    {
        if (fields == null || fields.Length == 0)
        {
            AresLogger.Log(nameof(SaveAsync), "The field list is null or empty.", severity: Severity.Error);
            return false;
        }

        if (_repository is not { } repository)
        {
            AresLogger.Log(nameof(SaveAsync), "User data is null. Unable to save fields.", severity: Severity.Error);
            return false;
        }

        try
        {
            foreach (string field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    AresLogger.Log(nameof(SaveAsync), "The field list contains a null or empty value.", severity: Severity.Error);
                    continue;
                }

                await repository.UpdateAsync(user, field);
            }

            return true;
        }
        catch (Exception ex)
        {
            AresLogger.Log(nameof(SaveAsync), "Error updating one or more fields in the database.", severity: Severity.Error, extra: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Updates the chat data of the user in the database.
    /// </summary>
    /// <param name="user">The user to save the chat data.</param>
    /// <param name="chat">Object containing the guild's chat data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public static async Task<bool> SaveChatDataAsync(User user, UserChat? chat = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (chat != null)
        {
            user.Chat = chat;
        }

        return await SaveAsync(user, "chat");
    }

    /// <summary>
    /// Adds new information to the database for a specific user.
    /// </summary>
    /// <param name="user">User to update in the database.</param>
    /// <param name="infos">List of information to be added.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveInfoAsync(User user, ulong guildId, List<UserChatInfo> infos, bool onlyCached = false)
    {
        user.Chat.Infos[guildId] = infos;
        return !onlyCached ? await SaveChatDataAsync(user) : true;
    }

    /// <summary>
    /// Saves chat history information for a specific user.
    /// </summary>
    /// <param name="user">The user to save the chat history.</param>
    /// <param name="guildId">The guild id to save history.</param>
    /// <param name="info">Chat information to be saved.</param>
    /// <returns>Returns true if the history was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveHistoricAsync(User user, ulong guildId, UserChatInfo info)
    {
        List<UserChatInfo>? list = ChatInfos(user, guildId);

        if (list == null) return await Task.FromResult(false);

        list.Add(info);

        return await SaveInfoAsync(user, guildId, list);
    }

    /// <summary>
    /// Updates chat information for a specific user and channel.
    /// </summary>
    /// <param name="user">The guild to update the chat information.</param>
    /// <param name="guildId">The user whose information should be updated.</param>
    /// <param name="info">The updated chat information.</param>
    /// <returns>Returns true if information was successfully updated, false otherwise.</returns>
    public static async Task<bool> UpdateChatInfoAsync(User user, ulong guildId, UserChatInfo info)
    {
        List<UserChatInfo>? infos = ChatInfos(user, guildId);

        if (infos == null)
        {
            infos = new List<UserChatInfo>();
            await SaveInfoAsync(user, guildId, infos, onlyCached: true);
        }

        UserChatInfo? existingInfo = infos.LastOrDefault(it => it.ChannelId.Equals(info.ChannelId));

        // It seems strange, but it is done so as not to add the same information as the chat.
        if (existingInfo != null && user.Chat.Infos.ContainsKey(guildId))
        {
            user.Chat.Infos[guildId].Remove(existingInfo);
        }

        user.Chat.Infos[guildId].Add(info);
        return await SaveChatDataAsync(user);
    }

    #region Conversation Info

    /// <summary>
    /// Retrieves the conversation history of the guild.
    /// </summary>
    /// <param name="user">The user to get the information.</param>
    /// <returns>Dictionary containing conversation histories or null if they don't exist.</returns>
    public static Dictionary<ulong, List<UserChatInfo>>? Infos(User user)
    {
        return user.Chat.Infos;
    }

    /// <summary>
    /// Retrieves the conversation history of the guild by user.
    /// </summary>
    public static List<UserChatHistoric>? AllChatHistorics(User user, ulong guildId)
    {
        List<UserChatInfo>? infos = Infos(user)?.GetValueOrDefault(guildId);
        if (infos == null) return null;

        return infos.SelectMany(info => info.Historics).ToList();
    }

    /// <summary>
    /// Retrieves chat history records for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guildId">The user to get chat history for.</param>
    /// <param name="channelId">Optional: Channel ID to filter history by. If 0, returns all channels.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public static List<UserChatHistoric>? ChatHistorics(User user, ulong guildId, ulong channelId = 0)
    {
        List<UserChatInfo>? infos = Infos(user)?.GetValueOrDefault(guildId);
        if (infos == null) return null;

        if (channelId != 0)
        {
            infos = infos.FindAll(historic => historic.ChannelId == channelId);
        }

        List<UserChatHistoric> historics = infos.SelectMany(info => info.Historics).ToList();

        return historics;
    }

    /// <summary>
    /// Retrieves all chat information for a specific user.
    /// </summary>
    /// <param name="user">The guild to get the information.</param>
    /// <param name="guildId">The user to get chat information for.</param>
    /// <returns>List of chat information objects or null if not found.</returns>
    public static List<UserChatInfo>? ChatInfos(User user, ulong guildId)
    {
        var infos = Infos(user);
        if (infos == null) return null;

        return infos.GetValueOrDefault(guildId);
    }

    /// <summary>
    /// Retrieves chat history records for a specific user in a specific channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="guildId">The user to get chat history for.</param>
    /// <param name="channelId">The channel ID to filter history by.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public static List<UserChatHistoric>? ChatHistoricsByChannel(User user, ulong guildId, ulong channelId)
    {
        List<UserChatHistoric>? historics = ChatHistorics(user, guildId, channelId: channelId);
        if (historics == null) return null;

        return historics;
    }

    /// <summary>
    /// Retrieves the latest chat information for a specific user in a specific channel.
    /// </summary>
    /// <param name="user">The user to get the information.</param>
    /// <param name="guildId">The guild id to get chat information for.</param>
    /// <param name="channelId">The channel ID to filter information by.</param>
    /// <returns>Chat information object or null if not found.</returns>
    public static UserChatInfo? ChatInfoByChannel(User user, ulong guildId, ulong channelId)
    {
        List<UserChatInfo>? userInfos = Infos(user)?.GetValueOrDefault(guildId);
        if (userInfos == null || !userInfos.Any()) return null;

        return userInfos.Find(historic => historic.ChannelId == channelId);
    }

    /// <summary>
    /// Toggles the active status of a chat for a specific user and channel.
    /// </summary>
    /// <param name="guild">The guild to toggle the chat information.</param>
    /// <param name="guildId">The user whose chat status should be toggled.</param>
    /// <param name="channelId">The channel ID for the chat.</param>
    /// <param name="active">The new active status: true to activate, false to deactivate.</param>
    /// <returns>Returns true if status was successfully changed, false otherwise.</returns>
    public static async Task<bool> ToggleChatInfo(User guild, ulong guildId, ulong channelId, bool active)
    {
        List<UserChatInfo>? infos = ChatInfos(guild, guildId);
        if (infos == null)
        {
            AresLogger.Log(nameof(ToggleChatInfo), "Unable to change the status of a chat information.", severity: Severity.Error);
            return false;
        }

        UserChatInfo? info = infos.LastOrDefault(i => i.ChannelId == channelId);

        if (info != null)
        {
            info.Active = active;

            if (!active)
            {
                ChatModelRepository? repository = AppCore.ChatModelRepository;
                if (repository == null) return false;

                // Delete cache if is not used more.
                await repository.DeleteCache(info.ModelId);
            }
        }

        return await SaveInfoAsync(guild, guildId, infos);
    }

    /// <summary>
    /// Retrieves the last chat history record for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="guildId">The user to get the last chat history for.</param>
    /// <param name="channelId">Optional: Channel ID to filter by. If 0, returns the last record from any channel.</param>
    /// <returns>The last chat history record or null if not found.</returns>
    public static UserChatHistoric? LastChatHistoric(User guild, ulong guildId, ulong channelId = 0)
    {
        if (channelId != 0)
        {
            return ChatInfoByChannel(guild, guildId, channelId)?.Historics.LastOrDefault();
        }

        return ChatHistorics(guild, guildId)?.LastOrDefault();
    }

    /// <summary>
    /// Retrieves the last chat information for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The guild to get the information.</param>
    /// <param name="guildId">The user to get the last chat information for.</param>
    /// <param name="channelId">Optional: Channel ID to filter by. If 0, returns the last information from any channel.</param>
    /// <returns>The last chat information or null if not found.</returns>
    public static UserChatInfo? LastChatInfo(User user, ulong guildId, ulong channelId = 0)
    {
        List<UserChatInfo>? infos = ChatInfos(user, guildId);

        if (infos == null || infos.Count == 0)
            return null;

        return channelId != 0 ? infos.FindAll(it => it.ChannelId == channelId).LastOrDefault() : infos.LastOrDefault();
    }

    /// <summary>
    /// Verify if a user is the owner of a chat in a specific channel.
    /// </summary>
    public static bool IsChatOwner(User user, ulong guildId, ulong channelId)
    {
        return ChatInfoByChannel(user, guildId, channelId) != null;
    }

    /// <summary>
    /// Creates new chat data for a user.
    /// </summary>
    /// <param name="user">The guild to create the chat data for.</param>
    /// <param name="guildId">The user to create chat data for.</param>
    /// <param name="info">The chat information to be created.</param>
    /// <returns>Returns true if chat data was successfully created, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static async Task<bool> CreateChatData(User user, ulong guildId, UserChatInfo info)
    {
        UserChat chat = user.Chat;

        if (chat == null)
        {
            AresLogger.Log(nameof(CreateChatData), "Guild chat data is null. Unable to create chat data for the user.", severity: Severity.Error);
            return await Task.FromResult(false);
        }

        try
        {
            if (!chat.Infos.TryGetValue(guildId, out List<UserChatInfo>? infos))
            {
                infos = new List<UserChatInfo>();
                chat.Infos[guildId] = infos;
            }

            if (info.Historics == null)
            {
                info.Historics = new List<UserChatHistoric>();
            }

            infos.Add(info);

            bool success = await SaveChatDataAsync(user, chat);

            if (success)
            {
                AresLogger.Log("Chat", $"Chat \"{info.Id}\" created by \"{guildId}\"");
            }

            return success;
        }
        catch (Exception ex)
        {
            AresLogger.Log(nameof(CreateChatData), "Error trying to create a chat history for the user.", severity: Severity.Error, extra: ex.Message);
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// Updates chat history records for a specific user and channel.
    /// </summary>
    /// <param name="user">The guild to update the chat history.</param>
    /// <param name="guildId">The user whose chat history should be updated.</param>
    /// <param name="channelId">The channel ID for the chat.</param>
    /// <param name="historics">The updated list of chat history records.</param>
    /// <returns>Returns true if history was successfully updated, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user or historics is null.</exception>
    public static async Task<bool> UpdateChatHistoricsAsync(User user, ulong guildId, ulong channelId, List<UserChatHistoric> historics)
    {
        if (historics == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), "Historics is null. Unable to update chat history.", severity: Severity.Error);
            return false;
        }

        if (user.Chat is not { } chat)
            return false;

        UserChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

        if (info == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {guildId} and channel {channelId}.", severity: Severity.Error);
            return false;
        }

        info.Historics = historics;

        user.Chat = chat;
        return await SaveChatDataAsync(user);
    }

    /// <summary>
    /// Adds a single chat history record for a specific user and channel.
    /// </summary>
    /// <param name="user">The guild to update the chat history.</param>
    /// <param name="guildId">The user to add chat history for.</param>
    /// <param name="channelId">The channel ID for the chat.</param>
    /// <param name="historic">The chat history record to add.</param>
    /// <returns>Returns true if history was successfully added, false otherwise.</returns>
    public static async Task<bool> UpdateChatHistoricsAsync(User user, ulong guildId, ulong channelId, UserChatHistoric historic)
    {
        UserChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

        if (info == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {guildId} and channel {channelId}.", severity: Severity.Error);
            return false;
        }

        List<UserChatHistoric> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), "Conversation historics are null.", severity: Severity.Error);
            return false;
        }

        historics.Add(historic);

        return await UpdateChatHistoricsAsync(user, guildId, channelId, historics);
    }

    /// <summary>
    /// Removes a specific conversation history record for a user and channel.
    /// </summary>
    /// <param name="user">The guild to remove the conversation from.</param>
    /// <param name="guildId">The user whose conversation record should be removed.</param>
    /// <param name="channelId">The channel ID for the conversation.</param>
    /// <param name="historic">The specific history record to remove.</param>
    /// <returns>Returns true if the record was successfully removed, false otherwise.</returns>
    public static async Task<bool> RemoveConversationAsync(User user, ulong guildId, ulong channelId, UserChatHistoric historic)
    {
        UserChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

        if (info == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {guildId} and channel {channelId}.", severity: Severity.Error);
            return false;
        }

        List<UserChatHistoric> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Log(nameof(RemoveConversationAsync), "Conversation historics are null.", severity: Severity.Error);
            return false;
        }

        historics.Remove(historic);

        return await UpdateChatHistoricsAsync(user, guildId, channelId, historics);
    }

    /// <summary>
    /// Checks if a user has an active conversation.
    /// </summary>
    /// <param name="user">The guild to check the conversation in.</param>
    /// <param name="guildId">The user to check.</param>
    /// <param name="channelId">Optional: Channel ID to filter by. If 0, returns the active check of the last chat used.</param>
    /// <returns>Returns true if an active conversation exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static bool HasActiveUserConversation(User user, ulong guildId, ulong channelId = 0)
    {
        Dictionary<ulong, List<UserChatInfo>>? infos = Infos(user);

        if (infos == null)
        {
            AresLogger.Log(nameof(HasActiveUserConversation), "Unable to get historical information.", severity: Severity.Error);
            return false;
        }

        if (!infos.TryGetValue(guildId, out List<UserChatInfo>? value) || value == null || value.Count == 0)
            return false;

        if (channelId == 0)
        {
            return value[value.Count - 1].Active;
        }

        UserChatInfo? chat = value.Find(x => x.ChannelId == channelId);
        return chat?.Active ?? value[value.Count - 1].Active;
    }

    /// <summary>
    /// Gets the count of conversations for a user.
    /// </summary>
    /// <param name="user">The guild to check the conversations in.</param>
    /// <param name="guildId">The user whose conversations are being queried.</param>
    /// <param name="active">Whether to filter by active conversations.</param>
    /// <returns>The number of conversations or -1 on error.</returns>
    public static int GetConversationsCount(User user, ulong guildId, bool active = false)
    {
        var infos = Infos(user);

        if (infos == null)
        {
            AresLogger.Log(nameof(GetConversationsCount), "Unable to get historical information.", severity: Severity.Error);
            return -1;
        }

        if (!infos.TryGetValue(guildId, out var userChats) || userChats == null)
            return 0;

        return userChats.Count(chat => chat.Active == active);
    }

    /// <summary>
    /// Gets the last chat model used by a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="user">The guild to get the model for.</param>
    /// <param name="guildId">The user to get the model for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last model from any channel.</param>
    /// <returns>The last chat model or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static async Task<ChatModel?> GetLastModelByUser(User user, ulong guildId, ulong channel = 0)
    {
        UserChatInfo? info = LastChatInfo(user, guildId, channelId: channel);
        if (info == null) return null;

        string model = info.ModelId;
        if (string.IsNullOrWhiteSpace(model)) return null;

        ChatModelRepository? repository = AppCore.ChatModelRepository;
        if (repository == null) return null;

        return await repository.FetchByNearestModelAsync(model, saveInRedis: true);
    }

    #endregion

    #region Conversation Snippet

    /// <summary>
    /// Updates the conversation historics for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to update the snippets for.</param>
    /// <param name="guildId">The guild ID to update the snippets in.</param>
    /// <param name="snippets">The updated list of snippets.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if the snippets were successfully updated, false otherwise.</returns>
    public static async Task<bool> UpdateSnippetsAsync(User user, ulong guildId, List<UserChatSnippet> snippets, bool onlyCached = false)
    {
        user.Chat.Snippets[guildId] = snippets;
        return !onlyCached ? await SaveChatDataAsync(user) : true;
    }

    /// <summary>
    /// Saves the conversation historics for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to save the snippets for.</param>
    /// <param name="guildId">The guild ID to save the snippets in.</param>
    /// <param name="snippets">The list of snippets to save.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if the snippets were successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveSnippetsAsync(User user, ulong guildId, List<UserChatSnippet> snippets, bool onlyCached = false)
    {
        List<UserChatSnippet>? saveSnippets = GetSnippetsByGuild(user, guildId);
        if (saveSnippets == null) return false;

        saveSnippets.AddRange(snippets);

        return await UpdateSnippetsAsync(user, guildId, saveSnippets, onlyCached: onlyCached);
    }

    /// <summary>
    /// Saves a new snippet for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to save the snippet for.</param>
    /// <param name="guildId">The guild ID to save the snippet in.</param>
    /// <param name="snippet">The snippet to be saved.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if the snippet was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveSnippetAsync(User user, ulong guildId, UserChatSnippet snippet, bool onlyCached = false)
    {
        user.Chat.Snippets ??= new();

        if (!user.Chat.Snippets.TryGetValue(guildId, out var snippets))
        {
            snippets = new List<UserChatSnippet>();
            user.Chat.Snippets[guildId] = snippets;
        }

        snippets.Add(snippet);

        return await SaveSnippetsAsync(user, guildId, snippets, onlyCached);
    }

    /// <summary>
    /// Removes a snippet by its ID for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to remove the snippet for.</param>
    /// <param name="guildId">The guild ID to remove the snippet from.</param>
    /// <param name="channelId">The channel ID to remove the snippet from.</param>
    /// <returns>Returns true if the snippet was successfully removed, false otherwise.</returns>
    public static async Task<bool> RemoveSnippetByChannelAsync(User user, ulong guildId, ulong channelId)
    {
        List<UserChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return false;

        snippets.RemoveAll(it => it.ChannelId == channelId);
        return await SaveSnippetsAsync(user, guildId, snippets);
    }

    /// <summary>
    /// Get all snippets for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to get the snippets for.</param>
    /// <param name="guildId">The guild ID to get the snippets from.</param>
    /// <returns>List of snippets or null if not found.</returns>
    public static List<UserChatSnippet>? GetSnippetsByGuild(User user, ulong guildId)
    {
        return user.Chat.Snippets.GetValueOrDefault(guildId);
    }

    /// <summary>
    /// Get all snippets for a specific user and guild, filtered by channel ID.
    /// </summary>
    /// <param name="user">The user to get the snippets for.</param>
    /// <param name="guildId">The guild ID to get the snippets from.</param>
    /// <param name="channelId">The channel ID to filter the snippets by.</param>
    /// <returns>List of snippets or null if not found.</returns>
    public static List<UserChatSnippet>? GetSnippetsByChannel(User user, ulong guildId, ulong channelId)
    {
        List<UserChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return null;

        return snippets.FindAll(it => it.ChannelId == channelId);
    }

    /// <summary>
    /// Get a specific snippet by its ID for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to get the snippet for.</param>
    /// <param name="guildId">The guild ID to get the snippet from.</param>
    /// <param name="id">The ID of the snippet to retrieve.</param>
    /// <returns>The snippet or null if not found.</returns>
    public static UserChatSnippet? GetSnippetById(User user, ulong guildId, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        List<UserChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return null;

        return snippets.Find(it => it.Id == id);
    }

    /// <summary>
    /// Get a specific snippet by its index for a specific user and guild.
    /// </summary>
    /// <param name="user">The user to get the snippet for.</param>
    /// <param name="guildId">The guild ID to get the snippet from.</param>
    /// <param name="index">The index of the snippet to retrieve.</param>
    /// <returns>The snippet or null if not found.</returns>
    public static UserChatSnippet? GetSnippetByIndex(User user, ulong guildId, ulong channelId, uint index)
    {
        List<UserChatSnippet>? snippets = GetSnippetsByChannel(user, guildId, channelId);
        if (snippets == null) return null;

        return snippets.Find(it => it.Index == index);
    }

    #endregion
}