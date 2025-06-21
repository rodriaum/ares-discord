// ... existing usings ...
using Ares.Common.Models.Chat;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Data;
using Ares.Common.Util;

namespace Ares.Discord.Services.Api;

public class UserService
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public UserService(HttpClient client, string baseUrl)
    {
        _client = client;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    #region UsersController

    public Task<User?> CreateOrGetUser(ulong id)
        => HttpUtil.PostAsync<User>(_client, $"{_baseUrl}/api/users/{id}/create-or-get", new { });

    public Task<User?> GetUser(ulong id, bool useCache = true)
        => HttpUtil.GetAsync<User>(_client, $"{_baseUrl}/api/users/{id}?useCache={useCache.ToString().ToLower()}");

    public Task<bool> UpdateUser(ulong id, User user, string field = "data")
        => HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/users/{id}/update?field={field}", user);

    public Task<List<User>?> GetAllUsers(int limit = 0)
        => HttpUtil.GetAsync<List<User>>(_client, $"{_baseUrl}/api/users/all?limit={limit}");

    public Task<bool> DeleteUser(ulong id)
        => HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/users/{id}");

    public Task<bool> DeleteUserCache(ulong id)
        => HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/users/{id}/cache/remove");

    public Task<bool> PersistUser(ulong id)
        => HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/users/{id}/cache/persist", new { });

    #endregion

    #region UserChatController

    public Task<bool> SaveChatData(ulong userId, UserChat chat)
        => HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/save-data", chat);

    public Task<bool> CreateChatData(ulong userId, ulong guildId, UserChatInfo info)
        => HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/create", info);

    public Task<List<UserChatInfo>?> GetChatInfos(ulong userId, ulong guildId)
        => HttpUtil.GetAsync<List<UserChatInfo>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/info");

    public Task<UserChatInfo?> GetChatInfoByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.GetAsync<UserChatInfo>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/info");

    public Task<List<UserChatHistoric>?> GetChatHistory(ulong userId, ulong guildId, ulong channelId = 0)
        => HttpUtil.GetAsync<List<UserChatHistoric>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/history?channelId={channelId}");

    public Task<bool> UpdateChatInfo(ulong userId, ulong guildId, UserChatInfo info)
        => HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/info", info);

    public Task<bool> ToggleChatStatus(ulong userId, ulong guildId, ulong channelId, bool active)
        => HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/toggle?active={active.ToString().ToLower()}", new { });

    public Task<bool> AddChatHistory(ulong userId, ulong guildId, ulong channelId, UserChatHistoric historic)
        => HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historic);

    public Task<bool> UpdateChatHistory(ulong userId, ulong guildId, ulong channelId, List<UserChatHistoric> historics)
        => HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historics);

    public Task<bool> RemoveConversation(ulong userId, ulong guildId, ulong channelId, UserChatHistoric historic)
        => HttpUtil.DeleteAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historic);

    public async Task<bool?> HasActiveConversation(ulong userId, ulong guildId, ulong channelId = 0)
    {
        var result = await HttpUtil.GetAsync<HasActiveConversationResponse>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/active?channelId={channelId}");
        return result?.hasActiveConversation;
    }

    public async Task<int?> GetConversationCount(ulong userId, ulong guildId, bool activeOnly = false)
    {
        var result = await HttpUtil.GetAsync<ConversationCountResponse>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/count?activeOnly={activeOnly.ToString().ToLower()}");
        return result?.count;
    }

    public Task<ChatModel?> GetLastModel(ulong userId, ulong guildId, ulong channelId = 0)
        => HttpUtil.GetAsync<ChatModel>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/last-model?channelId={channelId}");

    public async Task<bool?> IsChatOwner(ulong userId, ulong guildId, ulong channelId)
    {
        var result = await HttpUtil.GetAsync<IsOwnerResponse>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/is-owner");
        return result?.isOwner;
    }

    #endregion

    #region UserSnippetsController

    public Task<List<UserChatSnippet>?> GetSnippetsByGuild(ulong userId, ulong guildId)
        => HttpUtil.GetAsync<List<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}");

    public Task<List<UserChatSnippet>?> GetSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.GetAsync<List<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}");

    public Task<UserChatSnippet?> AddSnippet(ulong userId, ulong guildId, UserChatSnippet snippet)
        => HttpUtil.PostAsync<UserChatSnippet>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}", snippet);

    public Task<bool> RemoveSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.DeleteAsync<bool>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}");

    public Task<UserChatSnippet?> GetSnippetById(ulong userId, ulong guildId, string snippetId)
        => HttpUtil.GetAsync<UserChatSnippet>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/snippets/{snippetId}");

    public Task<UserChatSnippet?> GetSnippetByIndex(ulong userId, ulong guildId, ulong channelId, uint index)
        => HttpUtil.GetAsync<UserChatSnippet>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}/index/{index}");

    public Task<UserChatSnippet?> SaveSnippet(ulong userId, ulong guildId, UserChatSnippet snippet)
    => HttpUtil.PostAsync<UserChatSnippet>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}", snippet);

    public Task<List<UserChatSnippet>?> SaveSnippetsBatch(ulong userId, ulong guildId, List<UserChatSnippet> snippets)
    => HttpUtil.PostAsync<List<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/batch", snippets);

    #endregion
}