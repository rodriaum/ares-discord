using MongoDB.Driver.Linq;

namespace Ares.src.Service.Model
{
    public class ChatModel
    {
        public ModelCategory Category;
        public ModelType Type;
        public string DisplayName;
        public string Model;
        public bool Exclusive;

        public ChatModel(ModelCategory category, ModelType type, string displayName, string model, bool exclusive = false)
        {
            Category = category;
            Type = type;
            DisplayName = displayName;
            Model = model;
            Exclusive = exclusive;
        }

        public static List<ChatModel> GetModelsByCategory(ModelType category)
        {
            return Manager.OpenAiManager.OpenAiModels
                .Where(model => model.Type.Equals(category))
                .ToList();
        }

        public static ChatModel? GetByDisplayName(string displayName)
        {
            return Manager.OpenAiManager.OpenAiModels
                    .Where(model => model.DisplayName.Equals(displayName))
                    .First();
        }

        /// <summary>
        /// Retorna o modelo exato baseado no nome fornecido, sem considerar variações ou versões.
        /// O nome do modelo deve ser exato, incluindo a versão correta, como 'gpt-4-turbo'.
        /// Funciona para qualquer modelo, não limitado à categoria OpenAI, e a busca é sensível a maiúsculas/minúsculas.
        /// </summary>
        /// <param name="model">O nome exato do modelo a ser buscado.</param>
        /// <returns>O modelo correspondente ou null caso não encontrado.</returns>
        public static ChatModel? GetByModel(string model)
        {
            return Manager.OpenAiManager.OpenAiModels
                    .Where(it => it.Model.Equals(model, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
        }

        /// <summary>
        /// Retorna o modelo mais próximo baseado no nome fornecido. 
        /// É recomendado usar para se o <paramref name="model"/> for OpenAI. 
        /// O sistema pode retornar uma versão mais recente do modelo, como 'gpt-4-turbo-2024-04-09', 
        /// uma vez que a OpenAI utiliza sempre a versão mais recente do modelo disponível, 
        /// sem forçar ou especificar uma versão exata.
        /// </summary>
        /// <param name="model">O nome do modelo a ser buscado.</param>
        /// <returns>O modelo correspondente ou null caso não encontrado.</returns>
        public static ChatModel? GetByNearestModel(string model)
        {
            return Manager.OpenAiManager.OpenAiModels
                    .Where(it => model.StartsWith(it.Model))
                    .FirstOrDefault();
        }
    }
}