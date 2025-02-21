using Ares.src.Service.Model;

namespace Ares.src.Manager
{
    public class OpenAiManager
    {
        public static List<ChatModel> OpenAiModels = new List<ChatModel>();

        public void Init()
        {
            RegisterModels();
        }

        private static void RegisterModels()
        {
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Chat,  "GPT-4 Omni",      "gpt-4o"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Chat,  "GPT-4 Omni Mini", "gpt-4o-mini"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Chat,  "GPT-4 Turbo",     "gpt-4-turbo"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Chat,  "GPT-3 Turbo",     "gpt-3.5-turbo"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Image, "DALL·E 3",        "dall-e-3"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI, ModelType.Image, "DALL·E 2",        "dall-e-2"));
        }
    }
}