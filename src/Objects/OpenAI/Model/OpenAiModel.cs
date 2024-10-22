using Ares.src.Objects.OpenAI.Model.Category;
using Ares.src.OpenAi;

namespace Ares.src.Objects.OpenAI.Model
{
    internal class OpenAiModel
    {
        public OpenAiModelCategory OpenAiModelCategory;
        public string DisplayName;
        public string Model;
        public bool Exclusive;

        public OpenAiModel(OpenAiModelCategory openAiModelCategory, string displayName, string model, bool exclusive = false)
        {
            OpenAiModelCategory = openAiModelCategory;
            DisplayName = displayName;
            Model = model;
            Exclusive = exclusive;
        }

        public static List<OpenAiModel> GetModelsByCategory(OpenAiModelCategory category)
        {
            return OpenAiService.OpenAiModels
                .Where(model => model.OpenAiModelCategory.Equals(category))
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