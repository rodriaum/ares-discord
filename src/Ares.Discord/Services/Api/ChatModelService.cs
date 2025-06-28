using Ares.Common.DTOs;
using Ares.Common.Models.Data;
using Ares.Common.Util;
using System.Collections.Concurrent;

namespace Ares.Discord.Services.Api;

public class ChatModelService
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public ChatModelService(HttpClient client, string baseUrl)
    {
        _client = client;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task<ApiResult<ChatModel>?> CreateOrGetModel(string id)
    {
        return HttpUtil.PostAsync<ApiResult<ChatModel>>(_client, $"{_baseUrl}/api/chat-models/{id}/create-or-get", new { });
    }

    public Task<ApiResult<ChatModel>?> SaveModel(string id, ChatModel model)
    {
        return HttpUtil.PutAsync<ApiResult<ChatModel>>(_client, $"{_baseUrl}/api/chat-models/{id}", model);
    }

    public Task<ApiResult<ChatModel>?> GetModel(string id, bool saveInRedis = false)
    {
        return HttpUtil.GetAsync<ApiResult<ChatModel>>(_client, $"{_baseUrl}/api/chat-models/{id}?saveInRedis={saveInRedis.ToString().ToLower()}");
    }

    public Task<ApiResult<ChatModel>?> GetNearestModel(string id, bool saveInRedis = false)
    {
        return HttpUtil.GetAsync<ApiResult<ChatModel>>(_client, $"{_baseUrl}/api/chat-models/{id}/nearest?saveInRedis={saveInRedis.ToString().ToLower()}");
    }

    public Task<ApiResult<bool>?> UpdateModelField(string id, ChatModel model, string field)
    {
        return HttpUtil.PutAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/chat-models/{id}/update-field?field={field}", model);
    }

    public Task<ApiResult<bool>?> UpdateModelFields(string id, ChatModel model, string fields)
    {
        return HttpUtil.PutAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/chat-models/{id}/update-fields?fields={fields}", model);
    }

    public Task<ApiResult<ConcurrentBag<ChatModel>>?> GetAllModels(int limit = 0)
    {
        return HttpUtil.GetAsync<ApiResult<ConcurrentBag<ChatModel>>>(_client, $"{_baseUrl}/api/chat-models/all?limit={limit}");
    }

    public Task<ApiResult<object>?> DeleteModel(string id)
    {
        return HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/chat-models/{id}");
    }

    public Task<ApiResult<object>?> DeleteModelCache(string id)
    {
        return HttpUtil.DeleteAsync<ApiResult<object>>(_client, $"{_baseUrl}/api/chat-models/{id}/remove-cache");
    }

    public Task<ApiResult<bool>?> PersistModel(string id)
    {
        return HttpUtil.PostAsync<ApiResult<bool>>(_client, $"{_baseUrl}/api/chat-models/{id}/persist-cache", new { });
    }
}