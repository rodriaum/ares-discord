using Ares.src.Objects.OpenAI.Model;
using Ares.src.Objects.OpenAI.Model.Category;

namespace Ares.src.Manager
{
    public class OpenAiManager
    {
        public static List<OpenAiModel> OpenAiModels = new List<OpenAiModel>();

        public void Init()
        {
            RegisterModels();
        }

        private static void RegisterModels()
        {
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT,  "GPT-4 Omni",      "gpt-4o"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT,  "GPT-4 Omni Mini", "gpt-4o-mini"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT,  "GPT-4 Turbo",     "gpt-4-turbo"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT,  "GPT-3 Turbo",     "gpt-3.5-turbo"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.IMAGE, "DALL·E 3",        "dall-e-3"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.IMAGE, "DALL·E 2",        "dall-e-2"));
        }
    }
}