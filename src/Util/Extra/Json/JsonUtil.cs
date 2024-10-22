using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Ares.src.Util.Extra.Json
{
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

            return element.ToString(Newtonsoft.Json.Formatting.None);
        }

        public static T MapToObject<T>(Dictionary<string, string> map)
        {
            var jsonObject = new JObject();

            foreach (var kvp in map)
            {
                try
                {
                    jsonObject.Add(kvp.Key, JToken.Parse(kvp.Value));
                }
                catch (Exception)
                {
                    jsonObject.Add(kvp.Key, new JValue(kvp.Value));
                }
            }

            return jsonObject.ToObject<T>();
        }

        public static Dictionary<string, string> ObjectToMap(object src)
        {
            var map = new Dictionary<string, string>();

            try
            {
                var jsonObject = JObject.FromObject(src);
                foreach (var kvp in jsonObject)
                {
                    map.Add(kvp.Key, kvp.Value.ToString(Newtonsoft.Json.Formatting.None));
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
                var jsonObject = JObject.FromObject(src);
                foreach (var kvp in jsonObject)
                {
                    map.Add(kvp.Key, new List<string> { kvp.Value.ToString(Newtonsoft.Json.Formatting.None) });
                }
            }
            catch (Exception) { }

            return map;
        }
    }
}