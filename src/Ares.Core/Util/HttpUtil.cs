using System.Text;

namespace Ares.Core.Util;

public static class HttpUtil
{
    public static async Task<T?> GetAsync<T>(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        return await JsonUtil.StringToObjectAsync<T>(json);
    }

    public static async Task<T?> PostAsync<T>(HttpClient client, string url, object data)
    {
        string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        return await JsonUtil.StringToObjectAsync<T>(json);
    }

    public static async Task<T?> PutAsync<T>(HttpClient client, string url, object data)
    {
        string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PutAsync(url, content);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        return await JsonUtil.StringToObjectAsync<T>(json);
    }

    public static async Task<T?> DeleteAsync<T>(HttpClient client, string url, object? data = null)
    {
        HttpRequestMessage request = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(url)
        };

        if (data != null)
        {
            string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        return await JsonUtil.StringToObjectAsync<T>(json);
    }

    public static async Task<bool> DeleteAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }
}