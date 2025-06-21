/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Objects;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Common.Util;

public static class JsonUtil
{
    public static async Task<JsonNode?> JsonTreeAsync(object src)
    {
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, src);
            stream.Position = 0;
            return await JsonNode.ParseAsync(stream);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to generate JSON tree.", severity: Severity.Error, extra: ex.Message);
            return null;
        }
    }

    public static string ElementToString(JsonNode? element) =>
        element?.ToJsonString() ?? string.Empty;

    public static async Task<T?> DictionaryToObjectAsync<T>(
        Dictionary<string, string> map,
        JsonSerializerOptions? serializerOptions = null,
        JsonSerializerOptions? deserializeOptions = null)
    {
        try
        {
            var obj = new JsonObject();
            foreach (var kvp in map)
            {
                try
                {
                    obj[kvp.Key] = JsonNode.Parse(kvp.Value);
                }
                catch
                {
                    obj[kvp.Key] = kvp.Value;
                }
            }

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj, options: serializerOptions);
            stream.Position = 0;

            return await JsonSerializer.DeserializeAsync<T>(stream, options: deserializeOptions);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to convert dictionary to object.", severity: Severity.Error, extra: ex.Message);
            return default;
        }
    }

    public static async Task<Dictionary<string, string>> ObjectToDictionaryAsync(object src)
    {
        var map = new Dictionary<string, string>();
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, src);
            stream.Position = 0;

            if (await JsonNode.ParseAsync(stream) is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    map[kvp.Key] = kvp.Value?.ToJsonString() ?? "null";
                }
            }
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to convert object to dictionary.", severity: Severity.Error, extra: ex.Message);
        }

        return map;
    }

    public static async Task<Dictionary<string, List<string>>> ObjectToDictionaryListAsync(object src)
    {
        var map = new Dictionary<string, List<string>>();
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, src);
            stream.Position = 0;

            if (await JsonNode.ParseAsync(stream) is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    map[kvp.Key] = new List<string> { kvp.Value?.ToJsonString() ?? "null" };
                }
            }
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to convert object to dictionary list.", severity: Severity.Error, extra: ex.Message);
        }

        return map;
    }

    public static async Task<string> ObjectToStringAsync<T>(object value, JsonSerializerOptions? serializerOptions = null)
    {
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value, serializerOptions);
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to serialize object to string.", severity: Severity.Error, extra: ex.Message);
            return string.Empty;
        }
    }

    public static async Task<T?> StringToObjectAsync<T>(string jsonString)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to deserialize string to object.", severity: Severity.Error, extra: ex.Message);
            return default;
        }
    }

    public static async Task<T?> BytesToObjectAsync<T>(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to deserialize bytes to object.", severity: Severity.Error, extra: ex.Message);
            return default;
        }
    }
}