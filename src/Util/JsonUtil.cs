using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Ares.src.Util;

internal class JsonUtil
{
    public static JObject JsonTree(object src)
    {
        return JObject.FromObject(src);
    }

    public static object ElementToBson(JToken element)
    {

        switch (element.Type)
        {
            case JTokenType.String:
                return element.ToString();

            case JTokenType.Integer:
                return element.ToObject<int>();

            case JTokenType.Float:
                return element.ToObject<double>();

            case JTokenType.Boolean:
                return element.ToObject<bool>();
        }

        try
        {
            return BsonSerializer.Deserialize<BsonDocument>(element.ToString());
        }
        catch (Exception)
        {
            return BsonDocument.Parse(element.ToString());
        }
    }

    public static string ElementToString(JToken element)
    {
        if (element.Type == JTokenType.String)
        {
            return element.ToString();
        }

        return element.ToString(Formatting.None);
    }

    public static T? MapToObject<T>(Dictionary<string, string> map)
    {
        var obj = new JObject();

        foreach (var kvp in map)
        {
            try
            {
                obj.Add(kvp.Key, JToken.Parse(kvp.Value));
            }
            catch (Exception)
            {
                obj.Add(kvp.Key, new JValue(kvp.Value));
            }
        }

        return obj.ToObject<T>();
    }

    public static Dictionary<string, string> ObjectToMap(object src)
    {
        var map = new Dictionary<string, string>();

        try
        {
            var obj = JObject.FromObject(src);

            foreach (var kvp in obj)
            {
                if (kvp.Value != null)
                {
                    map.Add(kvp.Key, kvp.Value.ToString(Formatting.None));
                }
            }
        }
        catch (Exception) { }

        return map;
    }

    public static Dictionary<string, List<string>> ObjectToMapList(object src)
    {
        var map = new Dictionary<string, List<string>>();

        try
        {
            var obj = JObject.FromObject(src);
            foreach (var kvp in obj)
            {
                if (kvp.Value != null)
                {
                    map.Add(kvp.Key, new List<string> { kvp.Value.ToString(Formatting.None) });
                }
            }
        }
        catch (Exception) { }

        return map;
    }
}