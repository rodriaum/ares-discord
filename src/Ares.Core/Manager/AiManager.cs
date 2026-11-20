/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Core.Objects.Model;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Chat.Price;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using System.Collections.ObjectModel;

namespace Ares.Core.Manager;

public class AiManager
{
    private static readonly IReadOnlyCollection<ChatModel> _chatModels;

    public static IReadOnlyCollection<ChatModel> Models => _chatModels;

    static AiManager()
    {
        _chatModels = new ReadOnlyCollection<ChatModel>(InitializeModels());
    }

    private static List<ChatModel> InitializeModels()
    {
        AresLogger.Log("AI", "Starting AI model registration...");

        List<ChatModel> models = new List<ChatModel>
        {
            /*
             * OpenAI
             */

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-6 Astra",
                model: "gpt-6-astra",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00005m, inputPricePerToken: 0.00001m)
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-6.1 Sol",
                model: "gpt-6.1-sol",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.000002m)
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-6.1 Terra",
                model: "gpt-6.1-terra",
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.000005m, inputPricePerToken: 0.000001m)
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-6 Luna",
                model: "gpt-6-luna",
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000005m, inputPricePerToken: 0.0000001m)
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "GPT Image 2.6 Sunburst",
                model: "gpt-image-2.6",
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage()
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "GPT Image 2.6 Flare",
                model: "gpt-image-2.6",
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage()
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "GPT Image 2.5 Sunburst",
                model: "gpt-image-2.5",
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage()
            ),

            new(
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "GPT Image 2.5 Flare",
                model: "gpt-image-2.5",
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage()
            ),

            /*
             * Anthropic
             */

            new(
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude Fable 5.2",
                model: "claude-fable-5-2",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00004m, inputPricePerToken: 0.000008m)
            ),

            new(
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude Opus 5.5",
                model: "claude-opus-5-5",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.00002m, inputPricePerToken: 0.000004m)
            ),

            new(
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude Sonnet 5.5",
                model: "claude-sonnet-5-5",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.000002m)
            ),

            new(
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude Haiku 5.5",
                model: "claude-haiku-5-5",
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000005m, inputPricePerToken: 0.0000001m)
            ),

            /*
             * DeepSeek
             */

            new(
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "DeepSeek V4.1 Flash",
                model: "deepseek-flash",
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000006m, inputPricePerToken: 0.00000015m)
            ),

            new(
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "DeepSeek V4 Pro",
                model: "deepseek-v4-pro",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.00000198m, inputPricePerToken: 0.00000066m)
            ),

            /*
             * xAI
             */

            new(
                category: ModelCategory.xAI,
                type: ModelType.Chat,
                display: "Grok 4.7",
                model: "grok-4.7",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.000006m, inputPricePerToken: 0.000002m)
            ),

            /*
             * Google
             */

            new(
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 4 Carbon",
                model: "gemini-4-carbon",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage()
            ),

            new(
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 4 Argon",
                model: "gemini-4-argon",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage()
            ),

            new(
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 3.8 Flash",
                model: "gemini-3.8-flash",
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.00000375m, inputPricePerToken: 0.00000075m)
            )
        };

        Console.WriteLine();

        foreach (ChatModel model in models)
        {
            AresLogger.Log(
                $"AI: {model.Category}",
                $"Type \"{model.Type.ToString().ToLower()}\" with model \"{model.Model.ToLowerInvariant()}\" registered."
            );
        }

        Console.WriteLine();

        AresLogger.Log("AI", "Registered AI models.");

        return models;
    }
}
