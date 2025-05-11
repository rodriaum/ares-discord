/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Core.Util;

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

    public static object? ElementToBson(JsonNode? element)
    {
        if (element is JsonValue value)
        {
            if (value.TryGetValue(out string? str)) return str;
            if (value.TryGetValue(out int intVal)) return intVal;
            if (value.TryGetValue(out double dblVal)) return dblVal;
            if (value.TryGetValue(out bool boolVal)) return boolVal;
        }

        try
        {
            return BsonSerializer.Deserialize<BsonDocument>(element?.ToJsonString() ?? "{}");
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Fallback to BsonDocument.Parse due to deserialization error.", severity: Severity.Warning, extra: ex.Message);
            try
            {
                return BsonDocument.Parse(element?.ToJsonString() ?? "{}");
            }
            catch (Exception parseEx)
            {
                AresLogger.Log("JsonUtil", "Failed to parse JsonNode to BsonDocument.", severity: Severity.Error, extra: parseEx.Message);
                return null;
            }
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

    public static async Task<T?> BsonDocToObjectAsync<T>(
        BsonDocument document,
        JsonSerializerOptions? serializerOptions = null,
        JsonSerializerOptions? deserializeOptions = null)
    {
        if (document == null)
            return default;

        try
        {
            object mapped = BsonTypeMapper.MapToDotNetValue(document);
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, mapped, options: serializerOptions);
            stream.Position = 0;

            return await JsonSerializer.DeserializeAsync<T>(stream, options: deserializeOptions);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to deserialize BsonDocument.", severity: Severity.Error, extra: ex.Message);
            return default;
        }
    }

    public static async Task<BsonDocument?> ObjectToBsonDocumentAsync<T>(T obj)
    {
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;

            string json = Encoding.UTF8.GetString(stream.ToArray());
            return BsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            AresLogger.Log("JsonUtil", "Failed to serialize object to BsonDocument.", severity: Severity.Error, extra: ex.Message);
            return null;
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