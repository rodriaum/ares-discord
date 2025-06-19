using Ares.Core.Constants;
using Ares.Core.Models.Data;
using Ares.Core.Models.Language;
using Ares.Core.Objects;
using Ares.Core.Util;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Core.Manager;

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
    }

    public async Task Init()
    {
        foreach (LanguageCategory category in _languages)
        {
            try
            {
                bool isRunFilePath = true;

                // Run File Path
                string filePath = Path.Combine("lang", category.Code.ToLower() + ".json");

                if (!File.Exists(filePath))
                {
                    isRunFilePath = false;

                    // Project File Path
                    filePath = Path.Combine(AppConstants.ProjectPath, filePath);
                    if (!File.Exists(filePath)) continue;
                }

                if (AppConstants.AppDebugMode)
                {
                    AresLogger.Log(
                        "Lang",
                        $"Using lang from:",
                        severity: Severity.Debug,
                        extra: [$"Path: {filePath}", $"Run Path: {isRunFilePath}"]
                    );
                }

                using FileStream fileStream = File.OpenRead(filePath);
                using MemoryStream memoryStream = new MemoryStream();

                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                JsonDocumentOptions documentOptions = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                JsonNode? rootNode = await JsonNode.ParseAsync(memoryStream, documentOptions: documentOptions);
                if (rootNode == null) continue;

                Dictionary<string, string> messages = new Dictionary<string, string>();

                foreach (var property in rootNode.AsObject())
                {
                    messages[property.Key.ToLower()] = GetJsonMessage(property.Value);
                }

                _translation[category] = messages;
                AresLogger.Log("Lang", $"Registered lang \"{category.Code}\" with {messages.Count} translations.");
            }
            catch (Exception e)
            {
                AresLogger.Log(nameof(LanguageManager), $"Can't load language \"{category.Code}\"", severity: Severity.Error, extra: e.Message);
            }
        }
    }

    public List<LanguageCategory> GetLanguages() => _languages;

    static List<LanguageCategory> GetLanguageCategories()
    {
        return new List<LanguageCategory>
        {
            new("Português", "Português de Portugal", "pt"),
            new("English", "English", "en")
        };
    }

    static string GetJsonMessage(JsonNode? element)
    {
        if (element == null) return string.Empty;

        if (element is JsonArray array)
        {
            return string.Join("\n", array.Select(item => item?.ToString() ?? string.Empty));
        }

        return element.ToString() ?? string.Empty;
    }

    public bool IsKeyAvailable(LanguageCategory category, string key) =>
        category != null && key != null && _translation.TryGetValue(category, out var keys) && keys.ContainsKey(key.ToLower());

    public string GetTranslation(LanguageCategory category, string key)
    {
        if (!IsKeyAvailable(category, key)) return key;
        return _translation[category][key.ToLower()];
    }

    public string GetTranslation(Guild guild, string code)
    {
        LanguageCategory? category = LanguageCategory(guild);
        if (category == null) return code;

        return GetTranslation(category, code);
    }

    public LanguageCategory? GetCategoryByCode(string code) =>
        _languages.Find(category => category.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public LanguageCategory? LanguageCategory(Guild guild)
    {
        return GetCategoryByCode(guild.Preferences.Lang);
    }
}
