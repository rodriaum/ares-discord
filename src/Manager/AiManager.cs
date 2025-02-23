using System.Collections.ObjectModel;
using Ares.src.Service.Model;
using Ares.src.Utils;
using Ares.src.Utils.Extra;

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
        LogUtil.Log("AI", "Starting AI model registration...");

        List<ChatModel> models = new List<ChatModel>
        {
            // OpenAI Models
            new(category: ModelCategory.OpenAI, type: ModelType.Chat, display: "GPT-4 Omni", model: "gpt-4o", price: 0.0, available: true),
            new(category: ModelCategory.OpenAI, type: ModelType.Chat, display: "GPT-4 Omni Mini", model: "gpt-4o-mini", price: 0.0, available: true),
            new(category: ModelCategory.OpenAI, type: ModelType.Chat, display: "GPT-4 Turbo", model: "gpt-4-turbo", price: 0.0, available: true),
            new(category: ModelCategory.OpenAI, type: ModelType.Chat, display: "GPT-3 Turbo", model: "gpt-3.5-turbo", price: 0.0, available: true),
            new(category: ModelCategory.OpenAI, type: ModelType.Image, display: "DALL·E 3", model: "dall-e-3", price: 0.0, available: true),
            new(category: ModelCategory.OpenAI, type: ModelType.Image, display: "DALL·E 2", model: "dall-e-2", price: 0.0, available: true),

            // Anthropic Models
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude v2.1", model: "claude-2.1", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude v2.0", model: "claude-2.0", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude Instant v1.2", model: "claude-instant-1.2", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude 3 Opus", model: "claude-3-opus-20240229", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude 3 Sonnet", model: "claude-3-sonnet-20240229", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude 3.5 Sonnet", model: "claude-3-5-sonnet-20241022", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude 3.5 Haiku", model: "claude-3-5-haiku-20241022", price: 0.0, available: true),
            new(category: ModelCategory.Anthropic, type: ModelType.Chat, display: "Claude 3 Haiku", model: "claude-3-haiku-20240307", price: 0.0, available: true),

            // DeepSeek Models
            new(category: ModelCategory.DeepSeek, type: ModelType.Chat, display: "Deekseek V3", model: "deepseek-chat", price: 0.0, available: true),
            new(category: ModelCategory.DeepSeek, type: ModelType.Chat, display: "Deekseek R1", model: "deepseek-reasoner", price: 0.0, available: true),
        };

        // Empty Message
        Console.WriteLine();

        foreach (var model in models)
        {
            LogUtil.Log
                (
                    $"AI: {Util.CapitalizeFirstLetter(model.Category.ToString())}", 
                    $"Engine type \"{model.Type}\" with model \"{model.Model}\" registered."
                );
        }

        // Empty Message
        Console.WriteLine();

        LogUtil.Log("AI", "Registered AI models.");

        return models;
    }
}