using Ares.Objects.OpenAI.Model.Category;

namespace Ares.Objects.OpenAI
{
    internal class OpenAiModel
    {
        public OpenAiModelCategory OpenAiModelCategory;
        public string DisplayName;
        public string Model;
        public bool Exclusive;

        public OpenAiModel(OpenAiModelCategory openAiModelCategory, string displayName, string model, bool exclusive = false)
        {
            this.OpenAiModelCategory = openAiModelCategory;
            this.DisplayName = displayName;
            this.Model = model;
            this.Exclusive = exclusive;
        }

        public static List<OpenAiModel> GetModelsByCategory(OpenAiModelCategory category)
        {
            return OpenAi.OpenAiModels
                .Where(model => model.OpenAiModelCategory.Equals(category))
                .ToList();
        }

        public static OpenAiModel? GetByDisplayName(string displayName)
        {
            return OpenAi.OpenAiModels
                    .Where(model => model.DisplayName.Equals(displayName))
                    .First();
        }

        public static OpenAiModel? GetByModel(string model)
        {
            return OpenAi.OpenAiModels
                    .Where(it => it.Model.Equals(model))
                    .First();
        }
    }
}