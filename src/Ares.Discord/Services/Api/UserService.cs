using Ares.Common.DTOs;
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

    public Task<ApiResult<User>?> CreateOrGetUser(ulong id)
        => HttpUtil.PostAsync<ApiResult<User>>(_client, $"{_baseUrl}/api/users/{id}/create-or-get", new { });

    public Task<ApiResult<User>?> GetUser(ulong id, bool useCache = true)
        => HttpUtil.GetAsync<ApiResult<User>>(_client, $"{_baseUrl}/api/users/{id}?useCache={useCache.ToString().ToLower()}");

    public Task<ApiResult<object>?> UpdateUser(ulong id, User user, string field = "data")
        => HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{id}/update?field={field}", user);

    public Task<ApiResult<IEnumerable<User>>?> GetAllUsers(int limit = 0)
        => HttpUtil.GetAsync<ApiResult<IEnumerable<User>>>(_client, $"{_baseUrl}/api/users/all?limit={limit}");

    public Task<ApiResult<object>?> DeleteUser(ulong id)
        => HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{id}");

    public Task<ApiResult<object>?> DeleteUserCache(ulong id)
        => HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{id}/cache/remove");

    public Task<ApiResult<object>?> PersistUser(ulong id)
        => HttpUtil.PostAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{id}/cache/persist", new { });

    #endregion

    #region UserChatController

    public Task<ApiResult<object>?> SaveChatData(ulong userId, UserChat chat)
        => HttpUtil.PostAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/save-data", chat);

    public Task<ApiResult<object>?> CreateChatData(ulong userId, ulong guildId, UserChatInfo info)
        => HttpUtil.PostAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/create", info);

    public Task<ApiResult<IEnumerable<UserChatInfo>>?> GetChatInfos(ulong userId, ulong guildId)
        => HttpUtil.GetAsync<ApiResult<IEnumerable<UserChatInfo>>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/info");

    public Task<ApiResult<UserChatInfo>?> GetChatInfoByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.GetAsync<ApiResult<UserChatInfo>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/info");

    public Task<ApiResult<IEnumerable<UserChatHistoric>>?> GetChatHistory(ulong userId, ulong guildId, ulong channelId = 0)
        => HttpUtil.GetAsync<ApiResult<IEnumerable<UserChatHistoric>>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/history?channelId={channelId}");

    public Task<ApiResult<object>?> UpdateChatInfo(ulong userId, ulong guildId, UserChatInfo info)
        => HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/info", info);

    public Task<ApiResult<object>?> ToggleChatStatus(ulong userId, ulong guildId, ulong channelId, bool active)
        => HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/toggle?active={active.ToString().ToLower()}", new { });

    public Task<ApiResult<bool>?> AddChatHistory(ulong userId, ulong guildId, ulong channelId, UserChatHistoric historic)
        => HttpUtil.PostAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historic);

    public Task<ApiResult<object>?> UpdateChatHistory(ulong userId, ulong guildId, ulong channelId, List<UserChatHistoric> historics)
        => HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historics);

    public Task<ApiResult<object>?> RemoveConversation(ulong userId, ulong guildId, ulong channelId, UserChatHistoric historic)
        => HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/history", historic);

    public async Task<ApiResult<bool>?> HasActiveConversation(ulong userId, ulong guildId, ulong channelId = 0)
        => await HttpUtil.GetAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/active?channelId={channelId}");


    public async Task<ApiResult<int>?> GetConversationCount(ulong userId, ulong guildId, bool activeOnly = false)
        => await HttpUtil.GetAsync<ApiResult<int>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/count?activeOnly={activeOnly.ToString().ToLower()}");


    public Task<ApiResult<ChatModel>?> GetLastModel(ulong userId, ulong guildId, ulong channelId = 0)
        => HttpUtil.GetAsync<ApiResult<ChatModel>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/last-model?channelId={channelId}");

    public async Task<ApiResult<bool>?> IsChatOwner(ulong userId, ulong guildId, ulong channelId)
        => await HttpUtil.GetAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/users/{userId}/chat/guilds/{guildId}/channels/{channelId}/is-owner");

    #endregion

    #region UserSnippetsController

    public Task<ApiResult<IEnumerable<UserChatSnippet>>?> GetSnippetsByGuild(ulong userId, ulong guildId)
        => HttpUtil.GetAsync<ApiResult<IEnumerable<UserChatSnippet>>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}");

    public Task<ApiResult<IEnumerable<UserChatSnippet>>?> GetSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.GetAsync<ApiResult<IEnumerable<UserChatSnippet>>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}");

    public Task<ApiResult<UserChatSnippet>?> AddSnippet(ulong userId, ulong guildId, UserChatSnippet snippet)
        => HttpUtil.PostAsync<ApiResult<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}", snippet);

    public Task<ApiResult<object>?> RemoveSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
        => HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}");

    public Task<ApiResult<UserChatSnippet>?> GetSnippetById(ulong userId, ulong guildId, string snippetId)
        => HttpUtil.GetAsync<ApiResult<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/snippets/{snippetId}");

    public Task<ApiResult<UserChatSnippet>?> GetSnippetByIndex(ulong userId, ulong guildId, ulong channelId, uint index)
        => HttpUtil.GetAsync<ApiResult<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/channels/{channelId}/index/{index}");

    public Task<ApiResult<UserChatSnippet>?> SaveSnippet(ulong userId, ulong guildId, UserChatSnippet snippet)
    => HttpUtil.PostAsync<ApiResult<UserChatSnippet>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}", snippet);

    public Task<ApiResult<IEnumerable<UserChatSnippet>>?> SaveSnippetsBatch(ulong userId, ulong guildId, List<UserChatSnippet> snippets)
    => HttpUtil.PostAsync<ApiResult<IEnumerable<UserChatSnippet>>>(_client, $"{_baseUrl}/api/users/{userId}/snippets/guilds/{guildId}/batch", snippets);

    #endregion
}