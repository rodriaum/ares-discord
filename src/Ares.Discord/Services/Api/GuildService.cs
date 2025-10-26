using Ares.Common.DTOs;
using Ares.Common.Models.Data;
using Ares.Common.Models.Preference;
using Ares.Common.Models.Token;
using Ares.Common.Util;

namespace Ares.Discord.Services.Api;

public class GuildService
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public GuildService(HttpClient client, string baseUrl)
    {
        _client = client;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task<ApiResult<Guild>?> CreateOrGetGuild(ulong id)
    {
        return HttpUtil.PostAsync<ApiResult<Guild>>(_client, $"{_baseUrl}/api/guilds/{id}/create-or-get", new { });
    }

    public Task<ApiResult<Guild>?> GetGuild(ulong id, bool useCache = true)
    {
        return HttpUtil.GetAsync<ApiResult<Guild>>(_client, $"{_baseUrl}/api/guilds/{id}?useCache={useCache.ToString().ToLower()}");
    }

    public Task<ApiResult<object>?> UpdateGuild(ulong id, Guild guild, string field = "data")
    {
        return HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}/update?field={field}", guild);
    }

    public Task<ApiResult<object>?> SaveTokenData(ulong id, GToken token)
    {
        return HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}/token", token);
    }

    public Task<ApiResult<object>?> SavePreferenceData(ulong id, GPreference preferences)
    {
        return HttpUtil.PutAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}/preferences", preferences);
    }

    public Task<ApiResult<IEnumerable<Guild>>?> GetAllGuilds(int limit = 0)
    {
        return HttpUtil.GetAsync<ApiResult<IEnumerable<Guild>>>(_client, $"{_baseUrl}/api/guilds/all?limit={limit}");
    }

    public Task<ApiResult<object>?> DeleteGuild(ulong id)
    {
        return HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}");
    }

    public Task<ApiResult<object>?> DeleteGuildCache(ulong id)
    {
        return HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}/remove-cache");
    }

    public Task<ApiResult<object>?> PersistGuild(ulong id)
    {
        return HttpUtil.PostAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/guilds/{id}/persist-cache", new { });
    }

    public Task<ApiResult<string>?> GetLanguage(ulong id)
    {
        return HttpUtil.GetAsync<ApiResult<string>>(_client, $"{_baseUrl}/api/guilds/{id}/language");
    }

    public Task<ApiResult<IEnumerable<Guild>>?> GetByField(string fieldPath, string value, int limit = 0)
    {
        return HttpUtil.GetAsync<ApiResult<IEnumerable<Guild>>>(_client, $"{_baseUrl}/api/guilds/by-field?fieldPath={fieldPath}&value={value}&limit={limit}");
    }
}