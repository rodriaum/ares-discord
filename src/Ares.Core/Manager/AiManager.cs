/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Chat.Price;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using System.Collections.ObjectModel;

namespace Ares.Core.Manager;

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
        AresLogger.Log("AI", "Starting AI model registration...");

        List<ChatModel> models = new List<ChatModel>
        {

            // OpenAI Models
            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4.5 Preview",
                model: "gpt-4.5-preview",
                price: new ChatPriceUsage(output: 0.00015m, input: 0.000068m),
                exclusive: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4 o1 Preview",
                model: "o1-preview",
                price: new ChatPriceUsage(output: 0.0000044m, input: 0.000015m),
                exclusive: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4 o1",
                model: "o1",
                price: new ChatPriceUsage(output: 0.0000044m, input: 0.000015m),
                exclusive: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4 o1-mini",
                model: "o1-mini",
                price: new ChatPriceUsage(output: 0.0000006m, input: 0.0000011m),
                exclusive: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4 o3-mini",
                model: "o3-mini",
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
                    details: new List<ChatPriceUsageDetail>()
                    {
                        new ChatPriceUsageDetail(ImageQuality.High, ImageSize.W1024xH1792, 0.12m),
                        new ChatPriceUsageDetail(ImageQuality.High, ImageSize.W1024xH1024, 0.08m),
                        new ChatPriceUsageDetail(ImageQuality.Standard, ImageSize.W1024xH1792, 0.08m),
                        new ChatPriceUsageDetail(ImageQuality.Standard, ImageSize.W1024xH1024, 0.04m)
                    }
                ),
                available: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "DALL·E 2",
                model: "dall-e-2",
                price: new ChatPriceUsage(
                    details: new List<ChatPriceUsageDetail>()
                    {
                        new ChatPriceUsageDetail(ImageQuality.Standard, ImageSize.W1024xH1024, 0.016m),
                        new ChatPriceUsageDetail(ImageQuality.Standard, ImageSize.W512xH512, 0.018m),
                        new ChatPriceUsageDetail(ImageQuality.Standard, ImageSize.W256xH256, 0.02m)
                    }
                ),
                available: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.TTS,
                display: "TTS 1",
                model: "tts-1",
                price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
                available: true
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.TTS,
                display: "TTS 1 HD",
                model: "tts-1-hd",
                price: new ChatPriceUsage(output: 0.0m, input: 0.0m),
                exclusive: true
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

        foreach (ChatModel model in models)
        {
            AresLogger.Log(
                $"AI: {FormatterUtil.CapitalizeFirstLetter(model.Category.ToString())}",
                $"Engine type \"{model.Type}\" with model \"{model.Model}\" registered."
            );
        }

        // Empty Message
        Console.WriteLine();

        AresLogger.Log("AI", "Registered AI models.");

        return models;
    }

}