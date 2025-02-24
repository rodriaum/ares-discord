using System.Collections.ObjectModel;
using Ares.src.Service.Chat;
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
        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 OpenAI o1",
            model: "gpt-o1",
            price: new ChatPriceUsage(output: 0.0000044, input: 0.000015),
            available: false
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 OpenAI o3-mini",
            model: "gpt-o3-mini",
            price: new ChatPriceUsage(output: 0.0000006, input: 0.0000011),
            available: false
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Omni",
            model: "gpt-4o",
            price: new ChatPriceUsage(output: 0.00001, input: 0.0000025),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Omni Mini",
            model: "gpt-4o-mini",
            price: new ChatPriceUsage(output: 0.0000006, input: 0.00000015),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Turbo",
            model: "gpt-4-turbo",
            price: new ChatPriceUsage(output:0.00003, input: 0.00001),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-3 Turbo",
            model: "gpt-3.5-turbo",
            price: new ChatPriceUsage(output: 0.0000015, input: 0.0000005),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Image,
            display: "DALL·E 3",
            model: "dall-e-3",
            price: new ChatPriceUsage(
                detail: new ChatPriceUsageDetail(
                    imageStandard_1024_1024: 0.04,
                    ImageStandard_1024_1792: 0.08,
                    ImageHD_1024_1024: 0.08,
                    imageHD_1024_1792: 0.12
                )
            ),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Image,
            display: "DALL·E 2",
            model: "dall-e-2",
            price: new ChatPriceUsage(
                detail: new ChatPriceUsageDetail(
                    image_256_256: 0.016,
                    image_512_512: 0.018,
                    image_1024_1024: 0.02
                )
            ),
            available: true
        ),

        // Anthropic Models
        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude v2.1",
            model: "claude-2.1",
            price: new ChatPriceUsage(output: 0.0, input: 0.0),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude v2.0",
            model: "claude-2.0",
            price: new ChatPriceUsage(output: 0.0, input: 0.0),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude Instant v1.2",
            model: "claude-instant-1.2",
            price: new ChatPriceUsage(output: 0.0, input: 0.0),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Opus",
            model: "claude-3-opus-20240229",
            price: new ChatPriceUsage(output: 0.000075, input: 0.000015),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Sonnet",
            model: "claude-3-sonnet-20240229",
            price: new ChatPriceUsage(output: 0.0, input:0.0),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3.5 Sonnet",
            model: "claude-3-5-sonnet-20241022",
            price: new ChatPriceUsage(output: 0.000015, input: 0.000003),
            available: true
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3.5 Haiku",
            model: "claude-3-5-haiku-20241022",
            price: new ChatPriceUsage(output: 0.000004, input: 0.0000008),
            available: true
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Haiku",
            model: "claude-3-haiku-20240307",
            price: new ChatPriceUsage(output: 0.00000125, input: 0.00000025),
            available: true
        ),

        // DeepSeek Models
        new(
            category: ModelCategory.DeepSeek,
            type: ModelType.Chat,
            display: "Deepseek V3",
            model: "deepseek-chat",
            price: new ChatPriceUsage(output: 0.00000110, input: 0.00000027),
            available: true
        ),

        new(
            category: ModelCategory.DeepSeek,
            type: ModelType.Chat,
            display: "Deepseek R1",
            model: "deepseek-reasoner",
            price: new ChatPriceUsage(output: 0.00000219, input: 0.00000055),
            available: true
        )
        };

        // Empty Message
        Console.WriteLine();

        foreach (var model in models)
        {
            LogUtil.Log(
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