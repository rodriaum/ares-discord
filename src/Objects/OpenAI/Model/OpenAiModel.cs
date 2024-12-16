using Ares.src.Logging;
using Ares.src.Objects.OpenAI.Model.Category;

namespace Ares.src.Objects.OpenAI.Model
{
    public class OpenAiModel
    {
        public OpenAiModelCategory Category;
        public string DisplayName;
        public string Model;
        public bool Exclusive;

        public OpenAiModel(OpenAiModelCategory openAiModelCategory, string displayName, string model, bool exclusive = false)
        {
            Category = openAiModelCategory;
            DisplayName = displayName;
            Model = model;
            Exclusive = exclusive;
        }

        public static List<OpenAiModel> GetModelsByCategory(OpenAiModelCategory category)
        {
            return OpenAiService.OpenAiModels
                .Where(model => model.Category.Equals(category))
                .ToList();
        }

        public static OpenAiModel? GetByDisplayName(string displayName)
        {
            return OpenAiService.OpenAiModels
                    .Where(model => model.DisplayName.Equals(displayName))
                    .First();
        }

        public static OpenAiModel? GetByModel(string model)
        {
            return OpenAiService.OpenAiModels
                    .Where(it => it.Model.Equals(model))
                    .First();
        }
    }
}