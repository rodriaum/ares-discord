using System.Collections.ObjectModel;
using Ares.src.Objects.Chat;
using Ares.src.Objects.Model;
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
            price: new ChatPriceUsage(output: 0.0000044m, input: 0.000015m),
            exclusive: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 OpenAI o3-mini",
            model: "gpt-o3-mini",
            price: new ChatPriceUsage(output: 0.0000006m, input: 0.0000011m),
            exclusive: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Omni",
            model: "gpt-4o",
            price: new ChatPriceUsage(output: 0.00001m, input: 0.0000025m),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Omni Mini",
            model: "gpt-4o-mini",
            price: new ChatPriceUsage(output: 0.0000006m, input: 0.00000015m),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-4 Turbo",
            model: "gpt-4-turbo",
            price: new ChatPriceUsage(output:0.00003m, input: 0.00001m),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Chat,
            display: "GPT-3 Turbo",
            model: "gpt-3.5-turbo",
            price: new ChatPriceUsage(output: 0.0000015m, input: 0.0000005m),
            available: true
        ),

        new(
            category: ModelCategory.OpenAI,
            type: ModelType.Image,
            display: "DALL·E 3",
            model: "dall-e-3",
            price: new ChatPriceUsage(
                detail: new ChatPriceUsageDetail(
                    imageStandard_1024_1024: 0.04m,
                    ImageStandard_1024_1792: 0.08m,
                    ImageHD_1024_1024: 0.08m,
                    imageHD_1024_1792: 0.12m
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
                    image_256_256: 0.016m,
                    image_512_512: 0.018m,
                    image_1024_1024: 0.02m
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
            price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude v2.0",
            model: "claude-2.0",
            price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude Instant v1.2",
            model: "claude-instant-1.2",
            price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Opus",
            model: "claude-3-opus-20240229",
            price: new ChatPriceUsage(output: 0.000075m, input: 0.000015m),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Sonnet",
            model: "claude-3-sonnet-20240229",
            price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
            available: false
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3.7 Sonnet",
            model: "claude-3-7-sonnet-20250219",
            price: new ChatPriceUsage(output: 0.000015m, input: 0.000003m),
            exclusive: true
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3.5 Sonnet",
            model: "claude-3-5-sonnet-20241022",
            price: new ChatPriceUsage(output: 0.000015m, input: 0.000003m),
            available: true
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3.5 Haiku",
            model: "claude-3-5-haiku-20241022",
            price: new ChatPriceUsage(output: 0.000004m, input: 0.0000008m),
            available: true
        ),

        new(
            category: ModelCategory.Anthropic,
            type: ModelType.Chat,
            display: "Claude 3 Haiku",
            model: "claude-3-haiku-20240307",
            price: new ChatPriceUsage(output: 0.00000125m, input: 0.00000025m),
            available: true
        ),

        // DeepSeek Models
        new(
            category: ModelCategory.DeepSeek,
            type: ModelType.Chat,
            display: "Deepseek V3",
            model: "deepseek-chat",
            price: new ChatPriceUsage(output: 0.00000110m, input: 0.00000027m),
            available: true
        ),

        new(
            category: ModelCategory.DeepSeek,
            type: ModelType.Chat,
            display: "Deepseek R1",
            model: "deepseek-reasoner",
            price: new ChatPriceUsage(output: 0.00000219m, input: 0.00000055m),
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