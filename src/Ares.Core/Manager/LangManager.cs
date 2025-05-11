using Ares.Core.Constants;
using Ares.Core.Objects;
using Ares.Core.Objects.Language;
using Ares.Core.Util;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Core.Manager
{
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
        }

        public async Task Init()
        {
            foreach (LangCategory category in _languages)
            {
                try
                {
                    // Run File Path
                    string filePath = Path.Combine("lang", category.Code.ToLower() + ".json");

                    if (!File.Exists(filePath))
                    {
                        // Project File Path
                        filePath = Path.Combine(AresConstant.ProjectPath, filePath);
                        if (!File.Exists(filePath)) continue;
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
                    AresLogger.Log(nameof(LangManager), $"Can't load language \"{category.Code}\"", e.Message, severity: Severity.Error);
                }
            }
        }

        public List<LangCategory> GetLanguages() => _languages;

        static List<LangCategory> GetLanguageCategories()
        {
            return new List<LangCategory>
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

        public bool IsKeyAvailable(LangCategory category, string key) =>
            category != null && key != null && _translation.TryGetValue(category, out var keys) && keys.ContainsKey(key.ToLower());

        public string GetTranslation(LangCategory category, string key)
        {
            if (!IsKeyAvailable(category, key)) return key;
            return _translation[category][key.ToLower()];
        }

        public LangCategory? GetCategoryByCode(string code) =>
            _languages.Find(category => category.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}
