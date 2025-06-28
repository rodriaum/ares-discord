using Ares.Common.DTOs;
using Ares.Common.Util;

namespace Ares.Discord.Services.Api;

public class SystemService
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public SystemService(HttpClient client, string baseUrl)
    {
        _client = client;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    #region System Info

    public async Task<ApiResult<string>?> GetSystemStatus()
        => await HttpUtil.GetAsync<ApiResult<string>>(_client, $"{_baseUrl}/api/system/status");

    #endregion
}