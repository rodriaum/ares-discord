/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Language;
using Ares.Core.Util;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Ares.Core.Manager;

/// <summary>
/// Inspired by a legacy, complex Minecraft network design.
/// Original implementation: 
/// https://github.com/StudioLoad/Load/blob/main/core/src/main/java/br/com/loadmc/core/manager/LanguageManager.java
/// </summary>

public class LangManager
{
    readonly List<LangCategory> _languages = new List<LangCategory>();
    readonly Dictionary<LangCategory, Dictionary<string, string>> _translation = new Dictionary<LangCategory, Dictionary<string, string>>();

    public LangManager()
    {
        _languages = GetLanguageCategories();

        foreach (LangCategory category in _languages)
        {
            try
            {
                string filePath = Path.Combine("lang", category.Code.ToLower() + ".json");

                if (!File.Exists(filePath)) continue;

                string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                JObject language = JObject.Parse(jsonContent);

                Dictionary<string, string> messages = new Dictionary<string, string>();

                foreach (KeyValuePair<string, JToken?> entry in language)
                {
                    messages[entry.Key.ToLower()] = GetJsonMessage(entry.Value);
                }

                _translation[category] = messages;
                AresLogger.Log("Lang", $"Registered lang \"{category.Code}\" with {messages.Count} translations.");
            }
            catch (Exception e)
            {
                AresLogger.Error(nameof(LangManager), $"Can't load language {category.Name}: {e.Message}");
            }
        }
    }

    public List<LangCategory> GetLanguages()
    {
        return _languages;
    }

    static List<LangCategory> GetLanguageCategories()
    {
        return new List<LangCategory>
        {
            new("Português", "Português", "pt"),
            new("English", "English", "en")
        };
    }

    static string GetJsonMessage(JToken? element)
    {
        if (element == null) return string.Empty;

        return element.Type == JTokenType.Array
            ? string.Join("\n", element.Select(item => item.ToString()))
            : element.ToString();
    }

    public bool IsKeyAvailable(LangCategory category, string key)
    {
        return _translation.ContainsKey(category) && _translation[category].ContainsKey(key.ToLower());
    }

    public string GetTranslation(LangCategory category, string key)
    {
        if (!IsKeyAvailable(category, key)) return key;

        return _translation[category][key.ToLower()];
    }

    public LangCategory? GetCategoryByCode(string code)
    {
        return _languages.Find(category => category.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}