using System.Collections.ObjectModel;
using Ares.src.Service.Model;

namespace Ares.src.Manager;

public class AiManager
{
    private static readonly IReadOnlyCollection<ChatModel> _openAiModels;

    public static IReadOnlyCollection<ChatModel> Models => _openAiModels;

    static AiManager()
    {
        _openAiModels = new ReadOnlyCollection<ChatModel>(InitializeModels());
    }

    private static List<ChatModel> InitializeModels()
    {
        return new List<ChatModel>
        {
            // OpenAI Models
            new(ModelCategory.OpenAI, ModelType.Chat, "GPT-4 Omni", "gpt-4o"),
            new(ModelCategory.OpenAI, ModelType.Chat, "GPT-4 Omni Mini", "gpt-4o-mini"),
            new(ModelCategory.OpenAI, ModelType.Chat, "GPT-4 Turbo", "gpt-4-turbo"),
            new(ModelCategory.OpenAI, ModelType.Chat, "GPT-3 Turbo", "gpt-3.5-turbo"),
            new(ModelCategory.OpenAI, ModelType.Image, "DALL·E 3", "dall-e-3"),
            new(ModelCategory.OpenAI, ModelType.Image, "DALL·E 2", "dall-e-2"),

            // Anthropic Models
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude v2.1", "claude-2.1"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude v2.0", "claude-2.0"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude Instant v1.2", "claude-instant-1.2"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude 3 Opus", "claude-3-opus-20240229"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude 3 Sonnet", "claude-3-sonnet-20240229"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude 3.5 Sonnet", "claude-3-5-sonnet-20241022"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude 3.5 Haiku", "claude-3-5-haiku-20241022"),
            new(ModelCategory.Anthropic, ModelType.Chat, "Claude 3 Haiku", "claude-3-haiku-20240307")
        };
    }
}