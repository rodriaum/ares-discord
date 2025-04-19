/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Core.Util;

public class JsonUtil
{
    public static async Task<JsonNode?> JsonTreeAsync(object src)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, src);
        stream.Position = 0;

        return await JsonNode.ParseAsync(stream);
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
        catch (Exception)
        {
            return BsonDocument.Parse(element?.ToJsonString() ?? "{}");
        }
    }

    public static string ElementToString(JsonNode? element)
    {
        return element?.ToJsonString() ?? "";
    }

    public static async Task<T?> DictionaryToObjectAsync<T>
        (
            Dictionary<string, string> map,
            JsonSerializerOptions? serializerOptions = null,
            JsonSerializerOptions? deserializeOptions = null
        )
    {
        JsonObject obj = new JsonObject();

        foreach (var kvp in map)
        {
            try
            {
                obj[kvp.Key] = JsonNode.Parse(kvp.Value);
            }
            catch (Exception)
            {
                obj[kvp.Key] = kvp.Value;
            }
        }

        using MemoryStream stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, options: serializerOptions);
        stream.Position = 0;

        return await JsonSerializer.DeserializeAsync<T>(stream, options: deserializeOptions);
    }

    public static async Task<Dictionary<string, string>> ObjectToDictionaryAsync(object src)
    {
        Dictionary<string, string> map = new Dictionary<string, string>();

        try
        {
            using MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, src);
            stream.Position = 0;

            JsonObject? obj = await JsonNode.ParseAsync(stream) as JsonObject;

            if (obj != null)
            {
                foreach (var kvp in obj)
                {
                    map[kvp.Key] = kvp.Value?.ToJsonString() ?? "null";
                }
            }
        }
        catch (Exception) { }

        return map;
    }

    public static async Task<Dictionary<string, List<string>>> ObjectToDictionaryListAsync(object src)
    {
        Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();

        try
        {
            using MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, src);
            stream.Position = 0;

            JsonObject? obj = await JsonNode.ParseAsync(stream) as JsonObject;

            if (obj != null)
            {
                foreach (var kvp in obj)
                {
                    map[kvp.Key] = new List<string> { kvp.Value?.ToJsonString() ?? "null" };
                }
            }
        }
        catch (Exception) { }

        return map;
    }

    public static async Task<string> ObjectToStringAsync<T>(object value, JsonSerializerOptions? serializerOptions = null)
    {
        using MemoryStream stream = new MemoryStream();

        await JsonSerializer.SerializeAsync(stream, value, serializerOptions);
        stream.Position = 0;

        using StreamReader reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    public static async Task<T?> BsonDocToObjectAsync<T>(BsonDocument document)
    {
        try
        {
            object mapped = BsonTypeMapper.MapToDotNetValue(document);

            using MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, mapped);
            stream.Position = 0;


            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (JsonException ex)
        {
            AresLogger.Error("JsonException", "Error deserializing guild document.", ex.Message);
            return default(T);
        }
    }

    public static async Task<BsonDocument?> ObjectToBsonDocumentAsync<T>(T obj)
    {
        try
        {
            using MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;

            var json = Encoding.UTF8.GetString(stream.ToArray());
            return BsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            AresLogger.Error("JsonException", "Error serializing object to BsonDocument.", ex.Message);
            return null;
        }
    }

    public static async Task<T?> StringToObjectAsync<T>(string jsonString)
    {
        try
        {
            using var steam = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            return await JsonSerializer.DeserializeAsync<T>(steam);
        }
        catch (JsonException ex)
        {
            AresLogger.Error("JsonException", "Error deserializing string.", ex.Message);
            return default(T);
        }
    }

    public static async Task<T?> BytesToObjectAsync<T>(byte[] bytes)
    {
        try
        {
            using var steam = new MemoryStream(bytes);
            return await JsonSerializer.DeserializeAsync<T>(steam);
        }
        catch (JsonException ex)
        {
            AresLogger.Error("JsonException", "Error deserializing string.", ex.Message);
            return default(T);
        }
    }
}