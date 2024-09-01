using Discord;
using Discord_OpenAI.Objects.OpenAI;
using Discord_OpenAI.Objects.OpenAI.Model.Category;

namespace Discord_OpenAI.Manager
{
    internal class OpenAiManager : OpenAi
    {
        public void Init()
        {
            RegisterModels();
        }

        private static void RegisterModels()
        {
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT, "GPT-4 Omni", "gpt-4o"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT, "GPT-4 Omni Mini", "gpt-4o-mini"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT, "GPT-4 Turbo", "gpt-4-turbo"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.CHAT, "GPT-3 Turbo", "gpt-3-turbo"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.IMAGE, "DALL·E 3", "dall-e-3"));
            OpenAiModels.Add(new OpenAiModel(OpenAiModelCategory.IMAGE, "DALL·E 2", "dall-e-2"));
        }
    }
}