using Ares.src.Objects.Language;
using Ares.src.Utils.Extra;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Ares.src.Manager;

/// <summary>
/// Inspired by a legacy, complex Minecraft network design.
/// Original implementation: 
/// https://github.com/StudioLoad/Load/blob/main/core/src/main/java/br/com/loadmc/core/manager/LanguageManager.java
/// </summary>

public class LanguageManager
{
    readonly List<LanguageCategory> _languages = new List<LanguageCategory>();
    readonly Dictionary<LanguageCategory, Dictionary<string, string>> _translation = new Dictionary<LanguageCategory, Dictionary<string, string>>();

    public LanguageManager()
    {
        _languages = GetLanguageCategories();

        foreach (LanguageCategory category in _languages)
        {
            try
            {
                string fileName = "languages/" + category.Code.ToLower() + ".json";

                using (Stream? input = Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName))
                {
                    if (input == null) return;

                    JsonTextReader reader = new JsonTextReader(new StreamReader(input, Encoding.UTF8));
                    JObject language = (JObject)JToken.ReadFrom(reader);

                    Dictionary<string, string> messages = new Dictionary<string, string>();

                    foreach (var entry in language)
                    {
                        messages[entry.Key.ToLower()] = GetJsonMessage(entry.Value);
                    }

                    _translation[category] = messages;
                }
            }
            catch (Exception e)
            {
                LogUtil.Error(nameof(LanguageManager), $"Can't load language {category.Name}", e.Message);
            }
        }
    }

    public List<LanguageCategory> GetLanguages()
    {
        return _languages;
    }

    static List<LanguageCategory> GetLanguageCategories()
    {
        return new List<LanguageCategory>
        {
            new("Português", "Português", "pt"),
            new("English", "English", "en")
        };
    }

    static string GetJsonMessage(JToken? element)
    {
        if (element == null) return string.Empty;

        StringBuilder formatter = new StringBuilder();

        if (element.Type == JTokenType.Array)
        {
            JArray array = (JArray)element;

            for (int index = 0; index < array.Count; index++)
            {
                formatter.Append(array[index].ToString());

                if (index < array.Count - 1)
                    formatter.Append("\n");
            }
        }
        else
        {
            formatter.Append(element.ToString());
        }

        return formatter.ToString();
    }

    public bool IsKeyAvailable(LanguageCategory category, string key)
    {
        return _translation.ContainsKey(category) && _translation[category].ContainsKey(key.ToLower());
    }

    public string GetTranslation(LanguageCategory category, string key)
    {
        if (!IsKeyAvailable(category, key)) return key;

        return _translation[category][key.ToLower()];
    }

    public LanguageCategory? GetCategoryByCode(string code)
    {
        return _languages.Find(category => category.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}