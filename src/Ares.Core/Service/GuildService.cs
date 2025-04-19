/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models;
using Ares.Core.Models.Chat;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Models.Config;
using Ares.Core.Models.Token;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;

namespace Ares.Core.Service;

/// <summary>
/// Guild service to manage data and operations of an guild.
/// </summary>
public class GuildService
{
    /// <summary>
    /// Saves the specified fields of the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save.</param>
    /// <param name="fields">List of field names to be saved.</param>
    /// <returns>Returns true if fields were successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveAsync(Guild guild, List<string> fields)
    {
        if (fields == null || fields.Count == 0)
        {
            AresLogger.Error(nameof(SaveAsync), "The field list is null or empty.");
            return false;
        }

        if (AresCore.GuildRepository is not { } guildData)
        {
            AresLogger.Error(nameof(SaveAsync), "Guild data is null. Unable to save fields.");
            return false;
        }

        try
        {
            foreach (string field in fields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    AresLogger.Error(nameof(SaveAsync), "The field list contains a null or empty value.");
                    continue;
                }

                await guildData.UpdateAsync(guild, field);
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
    /// <param name="guild">The guild to save.</param>
    /// <param name="field">The field name to be saved.</param>
    /// <returns>Returns true if the field was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveAsync(Guild guild, string field)
    {
        return await SaveAsync(guild, new List<string> { field });
    }

    /// <summary>
    /// Saves token data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="token">Object containing guild token data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveTokenDataAsync(Guild guild, GTokenModel? token = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (token != null)
        {
            guild.Token = token;
        }

        return await SaveAsync(guild, "token");
    }

    /// <summary>
    /// Saves token data about the guild to the database.
    /// </summary>
    /// <param name="guild">The guild to save the config data.</param>
    /// <param name="config">Object containing guild token data.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveConfigDataAsync(Guild guild, GuildConfigData? config = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (config != null)
        {
            guild.Config = config;
        }

        return await SaveAsync(guild, "config");
    }

    /// <summary>
    /// Updates the chat data of the guild in the database.
    /// </summary>
    /// <param name="guild">The guild to save the chat data.</param>
    /// <param name="chat">Object containing the guild's chat data.</param>
    /// <returns>Returns true if data was successfully updated, false otherwise.</returns>
    public static async Task<bool> SaveChatDataAsync(Guild guild, GChatModel? chat = null)
    {
        // If is null, maybe it was probably modified in the variable itself, so it will save anyway.
        if (chat != null)
        {
            guild.Chat = chat;
        }

        return await SaveAsync(guild, "chat");
    }

    /// <summary>
    /// Adds new information to the database for a specific user.
    /// </summary>
    /// <param name="userId">User to update in the database.</param>
    /// <param name="infos">List of information to be added.</param>
    /// <param name="onlyCached">Optional: If true, data is stored locally instead of in the database.</param>
    /// <returns>Returns true if information was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveInfoAsync(Guild guild, ulong userId, List<GChatInfoModel> infos, bool onlyCached = false)
    {
        guild.Chat.Infos[userId] = infos;
        return !onlyCached ? await SaveChatDataAsync(guild) : true;
    }

    /// <summary>
    /// Saves chat history information for a specific user.
    /// </summary>
    /// <param name="guild">The guild to save the chat history.</param>
    /// <param name="userId">The user to save history for.</param>
    /// <param name="info">Chat information to be saved.</param>
    /// <returns>Returns true if the history was successfully saved, false otherwise.</returns>
    public static async Task<bool> SaveHistoricAsync(Guild guild, ulong userId, GChatInfoModel info)
    {
        List<GChatInfoModel>? list = ChatInfos(guild, userId);

        if (list == null) return await Task.FromResult(false);

        list.Add(info);

        return await SaveInfoAsync(guild, userId, list);
    }

    /// <summary>
    /// Updates chat information for a specific user and channel.
    /// </summary>
    /// <param name="guild">The guild to update the chat information.</param>
    /// <param name="userId">The user whose information should be updated.</param>
    /// <param name="info">The updated chat information.</param>
    /// <returns>Returns true if information was successfully updated, false otherwise.</returns>
    public static async Task<bool> UpdateChatInfoAsync(Guild guild, ulong userId, GChatInfoModel info)
    {
        List<GChatInfoModel>? infos = ChatInfos(guild, userId);

        if (infos == null)
        {
            infos = new List<GChatInfoModel>();
            await SaveInfoAsync(guild, userId, infos, onlyCached: true);
        }

        GChatInfoModel? existingInfo = infos.LastOrDefault(it => it.Channel.Equals(info.Channel));

        // It seems strange, but it is done so as not to add the same information as the chat.
        if (existingInfo != null && guild.Chat.Infos.ContainsKey(userId))
        {
            guild.Chat.Infos[userId].Remove(existingInfo);
        }

        guild.Chat.Infos[userId].Add(info);
        return await SaveChatDataAsync(guild);
    }

    /** Conversation System **/

    /// <summary>
    /// Retrieves the conversation history of the guild.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <returns>Dictionary containing conversation histories or null if they don't exist.</returns>
    public static Dictionary<ulong, List<GChatInfoModel>>? Infos(Guild guild)
    {
        return guild.Chat.Infos;
    }

    /// <summary>
    /// Retrieves chat history records for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="userId">The user to get chat history for.</param>
    /// <param name="channel">Optional: Channel ID to filter history by. If 0, returns all channels.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public static List<GChatHistoricModel>? ChatHistorics(Guild guild, ulong userId, ulong channel = 0)
    {
        List<GChatInfoModel>? infos = Infos(guild)?.GetValueOrDefault(userId);
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
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="userId">The user to get chat information for.</param>
    /// <returns>List of chat information objects or null if not found.</returns>
    public static List<GChatInfoModel>? ChatInfos(Guild guild, ulong userId)
    {
        var infos = Infos(guild);
        if (infos == null) return null;

        return infos.GetValueOrDefault(userId);
    }

    /// <summary>
    /// Retrieves chat history records for a specific user in a specific channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="userId">The user to get chat history for.</param>
    /// <param name="channel">The channel ID to filter history by.</param>
    /// <returns>List of chat history records or null if not found.</returns>
    public static List<GChatHistoricModel>? ChatHistoricsByChannel(Guild guild, ulong userId, ulong channel)
    {
        List<GChatHistoricModel>? historics = ChatHistorics(guild, userId, channel: channel);
        if (historics == null) return null;

        return historics;
    }

    /// <summary>
    /// Retrieves the latest chat information for a specific user in a specific channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="userId">The user to get chat information for.</param>
    /// <param name="channel">The channel ID to filter information by.</param>
    /// <returns>Chat information object or null if not found.</returns>
    public static GChatInfoModel? ChatInfoByChannel(Guild guild, ulong userId, ulong channel)
    {
        List<GChatInfoModel>? userInfos = Infos(guild)?.GetValueOrDefault(userId);
        if (userInfos == null || !userInfos.Any()) return null;

        return userInfos.Find(historic => historic.Channel == channel);
    }

    /// <summary>
    /// Toggles the active status of a chat for a specific user and channel.
    /// </summary>
    /// <param name="guild">The guild to toggle the chat information.</param>
    /// <param name="userId">The user whose chat status should be toggled.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="active">The new active status: true to activate, false to deactivate.</param>
    /// <returns>Returns true if status was successfully changed, false otherwise.</returns>
    public static Task<bool> ToggleChatInfo(Guild guild, ulong userId, ulong channel, bool active)
    {
        List<GChatInfoModel>? infos = ChatInfos(guild, userId);
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

        return SaveInfoAsync(guild, userId, infos);
    }

    /// <summary>
    /// Retrieves the last chat history record for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="userId">The user to get the last chat history for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last record from any channel.</param>
    /// <returns>The last chat history record or null if not found.</returns>
    public static GChatHistoricModel? LastChatHistoric(Guild guild, ulong userId, ulong channel = 0)
    {
        if (channel != 0)
        {
            return ChatInfoByChannel(guild, userId, channel)?.Historics.LastOrDefault();
        }

        return ChatHistorics(guild, userId)?.LastOrDefault();
    }

    /// <summary>
    /// Retrieves the last chat information for a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guild">The guild to get the information.</param>
    /// <param name="userId">The user to get the last chat information for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last information from any channel.</param>
    /// <returns>The last chat information or null if not found.</returns>
    public static GChatInfoModel? LastChatInfo(Guild guild, ulong userId, ulong channel = 0)
    {
        List<GChatInfoModel>? infos = ChatInfos(guild, userId);

        if (infos == null || infos.Count == 0)
            return null;

        return channel != 0 ? infos.FindAll(it => it.Channel == channel).LastOrDefault() : infos.LastOrDefault();
    }

    /// <summary>
    /// Creates new chat data for a user.
    /// </summary>
    /// <param name="guild">The guild to create the chat data for.</param>
    /// <param name="userId">The user to create chat data for.</param>
    /// <param name="info">The chat information to be created.</param>
    /// <returns>Returns true if chat data was successfully created, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static async Task<bool> CreateChatData(Guild guild, ulong userId, GChatInfoModel info)
    {
        GChatModel chat = guild.Chat;

        if (chat == null)
        {
            AresLogger.Error(nameof(CreateChatData), "Guild chat data is null. Unable to create chat data for the user.");
            return await Task.FromResult(false);
        }

        try
        {
            if (!chat.Infos.TryGetValue(userId, out List<GChatInfoModel>? infos))
            {
                infos = new List<GChatInfoModel>();
                chat.Infos[userId] = infos;
            }

            if (info.Historics == null)
            {
                info.Historics = new List<GChatHistoricModel>();
            }

            infos.Add(info);

            bool success = await SaveChatDataAsync(guild, chat);

            if (success)
            {
                AresLogger.Log("Chat", $"Chat \"{info.Id}\" created by \"{userId}\"");
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
    /// <param name="guild">The guild to update the chat history.</param>
    /// <param name="userId">The user whose chat history should be updated.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="historics">The updated list of chat history records.</param>
    /// <returns>Returns true if history was successfully updated, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user or historics is null.</exception>
    public static async Task<bool> UpdateChatHistoricsAsync(Guild guild, ulong userId, ulong channel, List<GChatHistoricModel> historics)
    {
        if (historics == null)
        {
            AresLogger.Error(nameof(UpdateChatHistoricsAsync), "Historics is null. Unable to update chat history.");
            return false;
        }

        if (guild.Chat is not { } chat)
            return false;

        GChatInfoModel? info = ChatInfoByChannel(guild, userId, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {userId} and channel {channel}.");
            return false;
        }

        info.Historics = historics;

        guild.Chat = chat;
        return await SaveChatDataAsync(guild);
    }

    /// <summary>
    /// Adds a single chat history record for a specific user and channel.
    /// </summary>
    /// <param name="guild">The guild to update the chat history.</param>
    /// <param name="userId">The user to add chat history for.</param>
    /// <param name="channel">The channel ID for the chat.</param>
    /// <param name="historic">The chat history record to add.</param>
    /// <returns>Returns true if history was successfully added, false otherwise.</returns>
    public static async Task<bool> UpdateChatHistoricsAsync(Guild guild, ulong userId, ulong channel, GChatHistoricModel historic)
    {
        GChatInfoModel? info = ChatInfoByChannel(guild, userId, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {userId} and channel {channel}.");
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Error(nameof(UpdateChatHistoricsAsync), "Conversation historics are null.");
            return false;
        }

        historics.Add(historic);

        return await UpdateChatHistoricsAsync(guild, userId, channel, historics);
    }

    /// <summary>
    /// Removes a specific conversation history record for a user and channel.
    /// </summary>
    /// <param name="guild">The guild to remove the conversation from.</param>
    /// <param name="userId">The user whose conversation record should be removed.</param>
    /// <param name="channel">The channel ID for the conversation.</param>
    /// <param name="historic">The specific history record to remove.</param>
    /// <returns>Returns true if the record was successfully removed, false otherwise.</returns>
    public static async Task<bool> RemoveConversationAsync(Guild guild, ulong userId, ulong channel, GChatHistoricModel historic)
    {
        GChatInfoModel? info = ChatInfoByChannel(guild, userId, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(UpdateChatHistoricsAsync), $"Cannot retrieve chat info for user ID {userId} and channel {channel}.");
            return false;
        }

        List<GChatHistoricModel> historics = info.Historics;

        if (historics == null)
        {
            AresLogger.Error(nameof(RemoveConversationAsync), "Conversation historics are null.");
            return false;
        }

        historics.Remove(historic);

        return await UpdateChatHistoricsAsync(guild, userId, channel, historics);
    }

    /// <summary>
    /// Checks if a user has an active conversation.
    /// </summary>
    /// <param name="guild">The guild to check the conversation in.</param>
    /// <param name="userId">The user to check.</param>
    /// <param name="channelId">Optional: Channel ID to filter by. If 0, returns the active check of the last chat used.</param>
    /// <returns>Returns true if an active conversation exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static bool HasActiveUserConversation(Guild guild, ulong userId, ulong channelId = 0)
    {
        Dictionary<ulong, List<GChatInfoModel>>? infos = Infos(guild);

        if (infos == null)
        {
            AresLogger.Error(nameof(HasActiveUserConversation), "Unable to get historical information.");
            return false;
        }

        if (!infos.TryGetValue(userId, out List<GChatInfoModel>? value) || value == null || value.Count == 0)
            return false;

        if (channelId == 0)
        {
            return value[value.Count - 1].Active;
        }

        GChatInfoModel? chat = value.Find(x => x.Channel == channelId);
        return chat?.Active ?? value[value.Count - 1].Active;
    }

    /// <summary>
    /// Gets the count of conversations for a user.
    /// </summary>
    /// <param name="guild">The guild to check the conversations in.</param>
    /// <param name="userId">The user whose conversations are being queried.</param>
    /// <param name="active">Whether to filter by active conversations.</param>
    /// <returns>The number of conversations or -1 on error.</returns>
    public static int GetConversationsCount(Guild guild, ulong userId, bool active = false)
    {
        var infos = Infos(guild);

        if (infos == null)
        {
            AresLogger.Error(nameof(GetConversationsCount), "Unable to get historical information.");
            return -1;
        }

        if (!infos.TryGetValue(userId, out var userChats) || userChats == null)
            return 0;

        return userChats.Count(chat => chat.Active == active);
    }

    /// <summary>
    /// Gets the last chat model used by a specific user, optionally filtered by channel.
    /// </summary>
    /// <param name="guild">The guild to get the model for.</param>
    /// <param name="userId">The user to get the model for.</param>
    /// <param name="channel">Optional: Channel ID to filter by. If 0, returns the last model from any channel.</param>
    /// <returns>The last chat model or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public static ChatModel? GetLastModelByUser(Guild guild, ulong userId, ulong channel = 0)
    {
        GChatInfoModel? info = LastChatInfo(guild, userId, channel: channel);
        if (info == null) return null;

        string model = info.Model;
        if (string.IsNullOrWhiteSpace(model)) return null;

        return ChatModel.GetByNearestModel(model);
    }

    /// <summary>
    /// Gets the language code configured for this guild.
    /// </summary>
    /// <param name="guild">The guild to get the language for.</param>
    /// <returns>The language code string.</returns>
    public static string Language(Guild guild)
    {
        return guild.Config.Lang;
    }

    /// <summary>
    /// Gets the language category object based on the guild's configured language.
    /// </summary>
    /// <param name="guild">The guild to get the language category for.</param>
    /// <returns>The language category object or null if not found.</returns>
    public static LangCategory? LangCategory(Guild guild)
    {
        return AresCore.LangManager.GetCategoryByCode(Language(guild));
    }

    /// <summary>
    /// Gets a translated string based on the guild's configured language.
    /// </summary>
    /// <param name="guild">The guild to get the translation for.</param>
    /// <param name="code">The translation code to look up.</param>
    /// <returns>The translated string or the original code if translation was not found.</returns>
    public static string GetTranslation(Guild guild, string code)
    {
        LangCategory? category = LangCategory(guild);
        if (category == null) return code;

        return AresCore.LangManager.GetTranslation(category, code);
    }
}