using Ares.Core.Models.Data;
using Ares.Core.Models.Preference;
using Ares.Core.Models.Token;
using Ares.Core.Util;

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

    public Task<Guild?> CreateOrGetGuild(ulong id)
    {
        return HttpUtil.PostAsync<Guild>(_client, $"{_baseUrl}/api/guilds/{id}/create-or-get", new { });
    }

    public Task<Guild?> GetGuild(ulong id, bool useCache = true)
    {
        return HttpUtil.GetAsync<Guild>(_client, $"{_baseUrl}/api/guilds/{id}?useCache={useCache.ToString().ToLower()}");
    }

    public Task<bool> UpdateGuild(ulong id, Guild guild, string field = "data")
    {
        return HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/guilds/{id}/update?field={field}", guild);
    }

    public Task<bool> SaveTokenData(ulong id, GToken token)
    {
        return HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/guilds/{id}/token", token);
    }

    public Task<bool> SavePreferenceData(ulong id, GPreference preferences)
    {
        return HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/guilds/{id}/preferences", preferences);
    }

    public Task<List<Guild>?> GetAllGuilds(int limit = 0)
    {
        return HttpUtil.GetAsync<List<Guild>>(_client, $"{_baseUrl}/api/guilds/all?limit={limit}");
    }

    public Task<bool> DeleteGuild(ulong id)
    {
        return HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/guilds/{id}");
    }

    public Task<bool> DeleteGuildCache(ulong id)
    {
        return HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/guilds/{id}/remove-cache");
    }

    public Task<bool> PersistGuild(ulong id)
    {
        return HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/guilds/{id}/persist-cache", new { });
    }

    public Task<string?> GetLanguage(ulong id)
    {
        return HttpUtil.GetAsync<string>(_client, $"{_baseUrl}/api/guilds/{id}/language");
    }

    public Task<List<Guild>?> GetByField(string fieldPath, string value, int limit = 0)
    {
        return HttpUtil.GetAsync<List<Guild>>(_client, $"{_baseUrl}/api/guilds/by-field?fieldPath={fieldPath}&value={value}&limit={limit}");
    }
}