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
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Chat,  "GPT-4 Omni",          "gpt-4o"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Chat,  "GPT-4 Omni Mini",     "gpt-4o-mini"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Chat,  "GPT-4 Turbo",         "gpt-4-turbo"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Chat,  "GPT-3 Turbo",         "gpt-3.5-turbo"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Image, "DALL·E 3",            "dall-e-3"));
            OpenAiModels.Add(new ChatModel(ModelCategory.OpenAI,    ModelType.Image, "DALL·E 2",            "dall-e-2"));

            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude v2.1",         "claude-2.1"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude v2.0",         "claude-2.0"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude Instant v1.2", "claude-instant-1.2"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude 3 Opus",       "claude-3-opus-20240229"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude 3 Sonnet",     "claude-3-sonnet-20240229"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude 3.5 Sonnet",   "claude-3-5-sonnet-20241022"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude 3.5 Haiku",    "claude-3-5-haiku-20241022"));
            OpenAiModels.Add(new ChatModel(ModelCategory.Anthropic, ModelType.Chat,  "Claude 3 Haiku",      "claude-3-haiku-20240307"));

        }
    }
}