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

    public Task<ChatModel?> CreateOrGetModel(string id)
    {
        return HttpUtil.PostAsync<ChatModel>(_client, $"{_baseUrl}/api/chat-models/{id}/create-or-get", new { });
    }

    public Task<ChatModel?> SaveModel(string id, ChatModel model)
    {
        return HttpUtil.PutAsync<ChatModel>(_client, $"{_baseUrl}/api/chat-models/{id}", model);
    }

    public Task<ChatModel?> GetModel(string id, bool saveInRedis = false)
    {
        return HttpUtil.GetAsync<ChatModel>(_client, $"{_baseUrl}/api/chat-models/{id}?saveInRedis={saveInRedis.ToString().ToLower()}");
    }

    public Task<ChatModel?> GetNearestModel(string id, bool saveInRedis = false)
    {
        return HttpUtil.GetAsync<ChatModel>(_client, $"{_baseUrl}/api/chat-models/{id}/nearest?saveInRedis={saveInRedis.ToString().ToLower()}");
    }

    public Task<bool> UpdateModelField(string id, ChatModel model, string field)
    {
        return HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/chat-models/{id}/update-field?field={field}", model);
    }

    public Task<bool> UpdateModelFields(string id, ChatModel model, string fields)
    {
        return HttpUtil.PutAsync<bool>(_client, $"{_baseUrl}/api/chat-models/{id}/update-fields?fields={fields}", model);
    }

    public Task<ConcurrentBag<ChatModel>?> GetAllModels(int limit = 0)
    {
        return HttpUtil.GetAsync<ConcurrentBag<ChatModel>>(_client, $"{_baseUrl}/api/chat-models/all?limit={limit}");
    }

    public Task<bool> DeleteModel(string id)
    {
        return HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/chat-models/{id}");
    }

    public Task<bool> DeleteModelCache(string id)
    {
        return HttpUtil.DeleteAsync(_client, $"{_baseUrl}/api/chat-models/{id}/remove-cache");
    }

    public Task<bool> PersistModel(string id)
    {
        return HttpUtil.PostAsync<bool>(_client, $"{_baseUrl}/api/chat-models/{id}/persist-cache", new { });
    }
}