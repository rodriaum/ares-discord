using Ares.src.Logging;
using Ares.src.Service.Model.Category;
using MongoDB.Driver.Linq;

namespace Ares.src.Service.Model
{
    public class OpenAiModel
    {
        public ModelCategory Category;
        public ModelType Type;
        public string DisplayName;
        public string Model;
        public bool Exclusive;

        public OpenAiModel(ModelCategory category, ModelType type, string displayName, string model, bool exclusive = false)
        {
            this.Category = category;
            this.Type = type;
            this.DisplayName = displayName;
            this.Model = model;
            this.Exclusive = exclusive;
        }

        public static List<OpenAiModel> GetModelsByCategory(ModelType category)
        {
            return Manager.OpenAiManager.OpenAiModels
                .Where(model => model.Type.Equals(category))
                .ToList();
        }

        public static OpenAiModel? GetByDisplayName(string displayName)
        {
            return Manager.OpenAiManager.OpenAiModels
                    .Where(model => model.DisplayName.Equals(displayName))
                    .First();
        }

        public static OpenAiModel? GetByModel(string model)
        {
            return Manager.OpenAiManager.OpenAiModels
                    .Where(it => it.Model.Equals(model))
                    .FirstOrDefault();
        }
    }
}