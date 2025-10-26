using Ares.Common.DTOs;
using Ares.Common.Objects;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Ares.Common.Util;

public static class HttpUtil
{
    /// <summary>
    /// Gets a human-readable description for an HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to describe.</param>
    /// <returns>A string description of the status code.</returns>
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
            _ => statusCode.ToString() ?? "Unknown"
        };
    }

    /// <summary>
    /// Performs an HTTP GET request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="client">The HttpClient instance to use for the request.</param>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <returns>The deserialized response object, or default if the request fails.</returns>
    public static async Task<T?> GetAsync<T>(HttpClient client, string url)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);

            string json = "";
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // Sometimes the API may return a non-OK status code but with a valid response body
                try
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    ApiResult<T?>? obj = await JsonUtil.StringToObjectAsync<ApiResult<T?>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (obj != null)
                    {
                        string statusDesc = GetStatusDescription(response.StatusCode);
                        string? desc = obj.Message;
                
                        await AresLogger.LogAsync(
                            nameof(HttpUtil),
                            $"Failed to GET from {url}",
                            severity: Severity.Error,
                            extra: [
                                $"Status: {(int)response.StatusCode} ({statusDesc})",
                                $"Message: {desc}"
                            ]
                        );
                    }
                }

                return default;
            }

            json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                await AresLogger.LogAsync(nameof(HttpUtil), $"Empty response body from GET {url}", severity: Severity.Warning);
                return default;
            }

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to GET from {url}",
                severity: Severity.Warning,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to GET from {url}: {ex.Message}", severity: Severity.Error);
            return default;
        }
    }

    /// <summary>
    /// Performs an HTTP POST request with JSON data and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="client">The HttpClient instance to use for the request.</param>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="data">The object to serialize and send in the request body.</param>
    /// <returns>The deserialized response object, or default if the request fails.</returns>
    public static async Task<T?> PostAsync<T>(HttpClient client, string url, object data)
    {
        try
        {
            string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
            StringContent content = new(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);

            string json = "";
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // Sometimes the API may return a non-OK status code but with a valid response body
                try
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    ApiResult<T?>? obj = await JsonUtil.StringToObjectAsync<ApiResult<T?>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (obj != null)
                    {
                        string statusDesc = GetStatusDescription(response.StatusCode);
                        string? desc = obj.Message;
                
                        await AresLogger.LogAsync(
                            nameof(HttpUtil),
                            $"Failed to POST to {url}",
                            severity: Severity.Error,
                            extra: [
                                $"Status: {(int)response.StatusCode} ({statusDesc})",
                                $"Message: {desc}"
                            ]
                        );
                    }
                }

                return default;
            }

            json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                await AresLogger.LogAsync(nameof(HttpUtil), $"Empty response body from POST {url}", severity: Severity.Warning);
                return default;
            }

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to POST to {url}",
                severity: Severity.Warning,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to POST to {url}: {ex.Message}", severity: Severity.Error);
            return default;
        }
    }

    /// <summary>
    /// Performs an HTTP PUT request with JSON data and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="client">The HttpClient instance to use for the request.</param>
    /// <param name="url">The URL to send the PUT request to.</param>
    /// <param name="data">The object to serialize and send in the request body.</param>
    /// <returns>The deserialized response object, or default if the request fails.</returns>
    public static async Task<T?> PutAsync<T>(HttpClient client, string url, object data)
    {
        try
        {
            string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(url, content);

            string json = "";
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // Sometimes the API may return a non-OK status code but with a valid response body
                try
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    ApiResult<T?>? obj = await JsonUtil.StringToObjectAsync<ApiResult<T?>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (obj != null)
                    {
                        string statusDesc = GetStatusDescription(response.StatusCode);
                        string? desc = obj.Message;
                
                        await AresLogger.LogAsync(
                            nameof(HttpUtil),
                            $"Failed to PUT to {url}",
                            severity: Severity.Error,
                            extra: [
                                $"Status: {(int)response.StatusCode} ({statusDesc})",
                                $"Message: {desc}"
                            ]
                        );
                    }
                }

                return default;
            }

            json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                await AresLogger.LogAsync(nameof(HttpUtil), $"Empty response body from PUT {url}", severity: Severity.Warning);
                return default;
            }

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to PUT to {url}",
                severity: Severity.Warning,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to PUT to {url}: {ex.Message}", severity: Severity.Error);
            return default;
        }
    }

    /// <summary>
    /// Performs an HTTP DELETE request with optional JSON data and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response into.</typeparam>
    /// <param name="client">The HttpClient instance to use for the request.</param>
    /// <param name="url">The URL to send the DELETE request to.</param>
    /// <param name="data">Optional object to serialize and send in the request body.</param>
    /// <returns>The deserialized response object, or default if the request fails.</returns>
    public static async Task<T?> DeleteAsync<T>(HttpClient client, string url, object? data = null)
    {
        try
        {
            // Validate and create URI safely
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                await AresLogger.LogAsync(nameof(HttpUtil), $"Invalid URL format: {url}", severity: Severity.Error);
                return default;
            }

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = uri
            };

            if (data != null)
            {
                string jsonData = await JsonUtil.ObjectToStringAsync<object>(data);
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(request);

            string json = "";
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // Sometimes the API may return a non-OK status code but with a valid response body
                try
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                catch (Exception)
                {
                    response.EnsureSuccessStatusCode();
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    ApiResult<T?>? obj = await JsonUtil.StringToObjectAsync<ApiResult<T?>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (obj != null)
                    {
                        string statusDesc = GetStatusDescription(response.StatusCode);
                        string? desc = obj.Message;
                
                        await AresLogger.LogAsync(
                            nameof(HttpUtil),
                            $"Failed to DELETE from {url}",
                            severity: Severity.Error,
                            extra: [
                                $"Status: {(int)response.StatusCode} ({statusDesc})",
                                $"Message: {desc}"
                            ]
                        );
                    }
                }

                return default;
            }

            json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                await AresLogger.LogAsync(nameof(HttpUtil), $"Empty response body from DELETE {url}", severity: Severity.Warning);
                return default;
            }

            return await JsonUtil.StringToObjectAsync<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null)
        {
            string statusDesc = GetStatusDescription(ex.StatusCode);

            await AresLogger.LogAsync(
                nameof(HttpUtil),
                $"Failed to DELETE from {url}",
                severity: Severity.Warning,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return default;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to DELETE from {url}: {ex.Message}", severity: Severity.Error);
            return default;
        }
    }

    /// <summary>
    /// Performs an HTTP DELETE request and returns a boolean indicating success or failure.
    /// </summary>
    /// <param name="client">The HttpClient instance to use for the request.</param>
    /// <param name="url">The URL to send the DELETE request to.</param>
    /// <returns>True if the request was successful (2xx status code), false otherwise.</returns>
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
                severity: Severity.Warning,
                extra: [
                    $"Status: {(int)ex.StatusCode} ({statusDesc})",
                    $"Exception: {ex.Message}"
                ]
            );
            return false;
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync(nameof(HttpUtil), $"Failed to DELETE from {url}: {ex.Message}", severity: Severity.Error);
            return false;
        }
    }
}