/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models;
using Ares.Core.Models.Chat;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Models.Collection;
using Ares.Core.Objects.Model;
using Ares.Core.Util;

namespace Ares.Core.Service;

/// <summary>
/// User service to manage data and operations.
/// </summary>
public class UserService
{
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

        if (AresCore.UserRepository is not { } repository)
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
            AresLogger.Log(nameof(SaveAsync), "Error updating one or more fields in the database.", ex.Message, severity: Severity.Error);
            return false;
        }
    }

    /// <summary>
    /// Updates the chat data of the user in the database.
    /// </summary>
    /// <param name="user">The user to save the chat data.</param>
    /// <param name="chat">Object containing the guild's chat data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public static async Task<bool> SaveChatDataAsync(User user, GChat? chat = null)
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
    public static async Task<bool> SaveInfoAsync(User user, ulong guildId, List<GChatInfo> infos, bool onlyCached = false)
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
    public static async Task<bool> SaveHistoricAsync(User user, ulong guildId, GChatInfo info)
    {
        List<GChatInfo>? list = ChatInfos(user, guildId);

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
    public static async Task<bool> UpdateChatInfoAsync(User user, ulong guildId, GChatInfo info)
    {
        List<GChatInfo>? infos = ChatInfos(user, guildId);

        if (infos == null)
        {
            infos = new List<GChatInfo>();
            await SaveInfoAsync(user, guildId, infos, onlyCached: true);
        }

        GChatInfo? existingInfo = infos.LastOrDefault(it => it.Channel.Equals(info.Channel));

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
    public static Dictionary<ulong, List<GChatInfo>>? Infos(User user)
    {
        return user.Chat.Infos;
    }

    /// <summary>
    /// Retrieves chat history records for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guildId">The user to get chat history for.</param>
    /// <param name="channelId">Optional: Channel ID to filter history by. If 0, returns all channels.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public static List<GChatHistoricModel>? ChatHistorics(User user, ulong guildId, ulong channelId = 0)
    {
        List<GChatInfo>? infos = Infos(user)?.GetValueOrDefault(guildId);
        if (infos == null) return null;

        if (channelId != 0)
        {
            infos = infos.FindAll(historic => historic.Channel == channelId);
        }

        List<GChatHistoricModel> historics = infos.SelectMany(info => info.Historics).ToList();

        return historics;
    }

    /// <summary>
    /// Retrieves all chat information for a specific user.
    /// </summary>
    /// <param name="user">The guild to get the information.</param>
    /// <param name="guildId">The user to get chat information for.</param>
    /// <returns>List of chat information objects or null if not found.</returns>
    public static List<GChatInfo>? ChatInfos(User user, ulong guildId)
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
    public static List<GChatHistoricModel>? ChatHistoricsByChannel(User user, ulong guildId, ulong channelId)
    {
        List<GChatHistoricModel>? historics = ChatHistorics(user, guildId, channelId: channelId);
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
    public static GChatInfo? ChatInfoByChannel(User user, ulong guildId, ulong channelId)
    {
        List<GChatInfo>? userInfos = Infos(user)?.GetValueOrDefault(guildId);
        if (userInfos == null || !userInfos.Any()) return null;

        return userInfos.Find(historic => historic.Channel == channelId);
    }

    /// <summary>
    /// Toggles the active status of a chat for a specific user and channel.
    /// </summary>
    /// <param name="guild">The guild to toggle the chat information.</param>
    /// <param name="guildId">The user whose chat status should be toggled.</param>
    /// <param name="channelId">The channel ID for the chat.</param>
    /// <param name="active">The new active status: true to activate, false to deactivate.</param>
    /// <returns>Returns true if status was successfully changed, false otherwise.</returns>
    public static Task<bool> ToggleChatInfo(User guild, ulong guildId, ulong channelId, bool active)
    {
        List<GChatInfo>? infos = ChatInfos(guild, guildId);
        if (infos == null)
        {
            AresLogger.Log(nameof(ToggleChatInfo), "Unable to change the status of a chat information.", severity: Severity.Error);
            return Task.FromResult(false);
        }

        GChatInfo? info = infos.LastOrDefault(i => i.Channel == channelId);

        if (info != null)
        {
            info.Active = active;
        }

        return SaveInfoAsync(guild, guildId, infos);
    }

    /// <summary>
    /// Retrieves the last chat history record for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="guildId">The user to get the last chat history for.</param>
    /// <param name="channelId">Optional: Channel ID to filter by. If 0, returns the last record from any channel.</param>
    /// <returns>The last chat history record or null if not found.</returns>
    public static GChatHistoricModel? LastChatHistoric(User guild, ulong guildId, ulong channelId = 0)
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
    public static GChatInfo? LastChatInfo(User user, ulong guildId, ulong channelId = 0)
    {
        List<GChatInfo>? infos = ChatInfos(user, guildId);

        if (infos == null || infos.Count == 0)
            return null;

        return channelId != 0 ? infos.FindAll(it => it.Channel == channelId).LastOrDefault() : infos.LastOrDefault();
    }

    /// <summary>
    /// Creates new chat data for a user.
    /// </summary>
    /// <param name="user">The guild to create the chat data for.</param>
    /// <param name="guildId">The user to create chat data for.</param>
    /// <param name="info">The chat information to be created.</param>
    /// <returns>Returns true if chat data was successfully created, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static async Task<bool> CreateChatData(User user, ulong guildId, GChatInfo info)
    {
        GChat chat = user.Chat;

        if (chat == null)
        {
            AresLogger.Log(nameof(CreateChatData), "Guild chat data is null. Unable to create chat data for the user.", severity: Severity.Error);
            return await Task.FromResult(false);
        }

        try
        {
            if (!chat.Infos.TryGetValue(guildId, out List<GChatInfo>? infos))
            {
                infos = new List<GChatInfo>();
                chat.Infos[guildId] = infos;
            }

            if (info.Historics == null)
            {
                info.Historics = new List<GChatHistoricModel>();
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
            AresLogger.Log(nameof(CreateChatData), "Error trying to create a chat history for the user.", ex.Message, severity: Severity.Error);
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
    public static async Task<bool> UpdateChatHistoricsAsync(User user, ulong guildId, ulong channelId, List<GChatHistoricModel> historics)
    {
        if (historics == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), "Historics is null. Unable to update chat history.", severity: Severity.Error);
            return false;
        }

        if (user.Chat is not { } chat)
            return false;

        GChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

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
    public static async Task<bool> UpdateChatHistoricsAsync(User user, ulong guildId, ulong channelId, GChatHistoricModel historic)
    {
        GChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

        if (info == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {guildId} and channel {channelId}.", severity: Severity.Error);
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

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
    public static async Task<bool> RemoveConversationAsync(User user, ulong guildId, ulong channelId, GChatHistoricModel historic)
    {
        GChatInfo? info = ChatInfoByChannel(user, guildId, channelId);

        if (info == null)
        {
            AresLogger.Log(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {guildId} and channel {channelId}.", severity: Severity.Error);
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

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
        Dictionary<ulong, List<GChatInfo>>? infos = Infos(user);

        if (infos == null)
        {
            AresLogger.Log(nameof(HasActiveUserConversation), "Unable to get historical information.", severity: Severity.Error);
            return false;
        }

        if (!infos.TryGetValue(guildId, out List<GChatInfo>? value) || value == null || value.Count == 0)
            return false;

        if (channelId == 0)
        {
            return value[value.Count - 1].Active;
        }

        GChatInfo? chat = value.Find(x => x.Channel == channelId);
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
    public static ChatModel? GetLastModelByUser(User user, ulong guildId, ulong channel = 0)
    {
        GChatInfo? info = LastChatInfo(user, guildId, channelId: channel);
        if (info == null) return null;

        string model = info.Model;
        if (string.IsNullOrWhiteSpace(model)) return null;

        return ChatModel.GetByNearestModel(model);
    }

    #endregion

    #region Conversation Snippet

    public static async Task<bool> UpdateSnippetsAsync(User user, ulong guildId, List<GChatSnippet> snippets, bool onlyCached = false)
    {
        user.Chat.Snippets[guildId] = snippets;
        return !onlyCached ? await SaveChatDataAsync(user) : true;
    }

    public static async Task<bool> SaveSnippetAsync(User user, ulong guildId, GChatSnippet snippet, bool onlyCached = false)
    {
        List<GChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);

        if (snippets == null)
        {
            snippets = new List<GChatSnippet>();
        }

        user.Chat.Snippets[guildId] = snippets;

        return await UpdateSnippetsAsync(user, guildId, snippets, onlyCached);
    }

    public static async Task<bool> RemoveSnippetByChannelAsync(User user, ulong guildId, ulong channelId)
    {
        List<GChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return false;

        snippets.RemoveAll(it => it.ChannelId == channelId);
        return await UpdateSnippetsAsync(user, guildId, snippets);
    }

    public static List<GChatSnippet>? GetSnippetsByGuild(User user, ulong guildId)
    {
        return user.Chat.Snippets.GetValueOrDefault(guildId);
    }

    public static List<GChatSnippet>? GetSnippetsByChannel(User user, ulong guildId, ulong channelId)
    {
        List<GChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return null;

        return snippets.FindAll(it => it.ChannelId == channelId);
    }


    public static GChatSnippet? GetSnippet(User user, ulong guildId, string id)
    {
        List<GChatSnippet>? snippets = GetSnippetsByGuild(user, guildId);
        if (snippets == null) return null;

        return snippets.Find(it => it.Id == id);
    }

    #endregion
}