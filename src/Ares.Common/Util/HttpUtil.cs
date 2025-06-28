using Ares.Common.Objects;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Ares.Common.Util;

public static class HttpUtil
{
    private static string GetStatusDescription(HttpStatusCode? statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.BadGateway => "Bad Gateway",
            HttpStatusCode.ServiceUnavailable => "Service Unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway Timeout",
            null => "No Status Code",
            _ => statusCode.ToString()
        };
    }

    public static async Task<T?> GetAsync<T>(HttpClient client, string url)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to GET from {url}",
                severity: Severity.Error,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to GET from {url}", severity: Severity.Error);
            return default;
        }
    }

    public static async Task<T?> PostAsync<T>(HttpClient client, string url, object data)
    {
        try
        {
            string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to POST to {url}",
                severity: Severity.Error,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to POST to {url}", severity: Severity.Error);
            return default;
        }
    }

    public static async Task<T?> PutAsync<T>(HttpClient client, string url, object data)
    {
        try
        {
            string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to PUT to {url}",
                severity: Severity.Error,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to PUT to {url}", severity: Severity.Error);
            return default;
        }
    }

    public static async Task<T?> DeleteAsync<T>(HttpClient client, string url, object? data = null)
    {
        try
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
            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to DELETE from {url}",
                severity: Severity.Error,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to DELETE from {url}", severity: Severity.Error);
            return default;
        }
    }

    public static async Task<bool> DeleteAsync(HttpClient client, string url)
    {
        try
        {
            HttpResponseMessage response = await client.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to DELETE from {url}",
                severity: Severity.Error,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return false;
        }
        catch (Exception)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to DELETE from {url}", severity: Severity.Error);
            return false;
        }
    }
}