using Discord_OpenAI.Objects.OpenAI.Model.Category;

namespace Discord_OpenAI.Objects.OpenAI
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
    }
}