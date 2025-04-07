/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Core.Objects.Model;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Chat.Price;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using OllamaSharp;
using OllamaSharp.Models;
using System.Collections.ObjectModel;

namespace Ares.Core.Manager;

public class AiManager
{
    private static IReadOnlyCollection<ChatModel> _chatModels;

    public static IReadOnlyCollection<ChatModel> Models => _chatModels;

    static AiManager()
    {
        _chatModels = new ReadOnlyCollection<ChatModel>(new List<ChatModel>());
    }

    public async Task Init()
    {
        _chatModels = new ReadOnlyCollection<ChatModel>(await InitializeModels());
    }

    private async static Task<List<ChatModel>> InitializeModels()
    {
        AresLogger.Log("AI", "Starting AI model registration...");

        List<ChatModel> models = new List<ChatModel>
        {

            /*
             * OpenAI Models
             */

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4.5 Preview",
                model: "gpt-4.5-preview",
                descriptionKey: LangKeys.ModelDescGpt45,
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00015m, inputPricePerToken: 0.000068m),
                exclusive: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "o1 Pro",
                model: "o1-pro",
                descriptionKey: LangKeys.ModelDescO1Pro,
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.0006m, inputPricePerToken: 0.00015m),
                exclusive: true
            ),

            new(
                request : ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "o1",
                model: "o1",
                descriptionKey: LangKeys.ModelDescO1,
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000044m, inputPricePerToken: 0.000015m),
                exclusive: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "o1-mini",
                model: "o1-mini",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000006m, inputPricePerToken: 0.0000011m),
                available: true
            ),

            new(
                request : ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "o3-mini",
                model: "o3-mini",
                descriptionKey: LangKeys.ModelDescO3Mini,
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000006m, inputPricePerToken: 0.0000011m),
                available: true
            ),

            new(
                request : ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4o",
                model: "gpt-4o",
                descriptionKey: LangKeys.ModelDescGpt4o,
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.0000025m),
                available: true
            ),

            new(
                request : ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4o Mini",
                model: "gpt-4o-mini",
                descriptionKey: LangKeys.ModelDescGpt4oMini,
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000006m, inputPricePerToken: 0.00000015m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4o Search Preview",
                model: "gpt-4o-search-preview",
                descriptionKey: LangKeys.ModelDescGpt4oS,
                task: ModelTaskCategory.ToolSpecific,
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.0000025m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4o Mini Search Preview",
                model: "gpt-4o-mini-search-preview",
                task: ModelTaskCategory.ToolSpecific,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000006m, inputPricePerToken: 0.00000015m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-4 Turbo",
                model: "gpt-4-turbo",
                task: ModelTaskCategory.Older,
                price: new ChatPriceUsage(outputPricePerToken:0.00003m, inputPricePerToken: 0.00001m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Chat,
                display: "GPT-3 Turbo",
                model: "gpt-3.5-turbo",
                task: ModelTaskCategory.Older,
                price: new ChatPriceUsage(outputPricePerToken: 0.0000015m, inputPricePerToken: 0.0000005m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "DALL·E 3",
                model: "dall-e-3",
                task: ModelTaskCategory.Image,
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
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.Image,
                display: "DALL·E 2",
                model: "dall-e-2",
                task: ModelTaskCategory.Image,
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
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.TTS,
                display: "TTS 1",
                model: "tts-1",
                task: ModelTaskCategory.TTS,
                price: new ChatPriceUsage(outputPricePerToken: 0.000015m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.OpenAI,
                type: ModelType.TTS,
                display: "TTS 1 HD",
                model: "tts-1-hd",
                task: ModelTaskCategory.TTS,
                price: new ChatPriceUsage(outputPricePerToken: 0.00003m),
                exclusive: true
            ),

            /*
             * Anthropic Models
             */

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude v2.1",
                model: "claude-2.1",
                price: new ChatPriceUsage(outputPricePerToken: 0.0m, inputPricePerToken: 0.0m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude v2.0",
                model: "claude-2.0",
                price: new ChatPriceUsage(outputPricePerToken: 0.0m, inputPricePerToken: 0.0m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude Instant v1.2",
                model: "claude-instant-1.2",
                price: new ChatPriceUsage(outputPricePerToken: 0.0m, inputPricePerToken: 0.0m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3 Opus",
                model: "claude-3-opus-20240229",
                price: new ChatPriceUsage(outputPricePerToken: 0.000075m, inputPricePerToken: 0.000015m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3 Sonnet",
                model: "claude-3-sonnet-20240229",
                price: new ChatPriceUsage(outputPricePerToken: 0.0m, inputPricePerToken: 0.0m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3.7 Sonnet",
                model: "claude-3-7-sonnet-20250219",
                price: new ChatPriceUsage(outputPricePerToken: 0.000015m, inputPricePerToken: 0.000003m),
                exclusive: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3.5 Sonnet",
                model: "claude-3-5-sonnet-20241022",
                price: new ChatPriceUsage(outputPricePerToken: 0.000015m, inputPricePerToken: 0.000003m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3.5 Haiku",
                model: "claude-3-5-haiku-20241022",
                price: new ChatPriceUsage(outputPricePerToken: 0.000004m, inputPricePerToken: 0.0000008m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Anthropic,
                type: ModelType.Chat,
                display: "Claude 3 Haiku",
                model: "claude-3-haiku-20240307",
                price: new ChatPriceUsage(outputPricePerToken: 0.00000125m, inputPricePerToken: 0.00000025m),
                available: true
            ),

            /*
             * DeepSeek Models
             */

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "Deepseek V3",
                model: "deepseek-chat",
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(outputPricePerToken: 0.00000110m, inputPricePerToken: 0.00000027m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "Deepseek R1",
                model: "deepseek-reasoner",
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(outputPricePerToken: 0.00000219m, inputPricePerToken: 0.00000055m),
                available: true
            ),

            /*
             * xAI Models
             */

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.xAI,
                type: ModelType.Chat,
                display: "Grok 2",
                model: "grok-2-latest",
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.000002m),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.xAI,
                type: ModelType.Vision,
                display: "Grok 2 Vision",
                model: "grok-2-vision-latest",
                task: ModelTaskCategory.Vision,
                price: new ChatPriceUsage(outputPricePerToken: 0.00001m, inputPricePerToken: 0.000002m, inputPricePerImage: 0.000002m),
                available: false
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.xAI,
                type: ModelType.Image,
                display: "Grok 2 Image",
                model: "grok-2-image",
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage(),
                available: false
            ),

            /*
             * Google Models
             */

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 2.5 Pro Preview",
                model: "gemini-2.5-pro-preview-03-25",
                descriptionKey: LangKeys.ModelDescGemini25ProPreview,
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(),
                exclusive: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 2.0 Flash",
                model: "gemini-2.0-flash",
                descriptionKey: LangKeys.ModelDescGemini20Flash,
                task: ModelTaskCategory.Flagship,
                price: new ChatPriceUsage(),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 2.0 Flash-Lite",
                model: "gemini-2.0-flash-lite",
                descriptionKey: LangKeys.ModelDescGemini20FlashLite,
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 1.5 Flash",
                model: "gemini-1.5-flash",
                descriptionKey: LangKeys.ModelDescGemini15Flash,
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 1.5 Flash-8B",
                model: "gemini-1.5-flash-8b",
                descriptionKey: LangKeys.ModelDescGemini15Flash8B,
                task: ModelTaskCategory.CostOptimized,
                price: new ChatPriceUsage(),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Chat,
                display: "Gemini 1.5 Pro",
                model: "gemini-1.5-pro",
                descriptionKey: LangKeys.ModelDescGemini15Pro,
                task: ModelTaskCategory.Reasoning,
                price: new ChatPriceUsage(),
                available: true
            ),

            new(
                request: ChatRequestType.Web,
                category: ModelCategory.Google,
                type: ModelType.Image,
                display: "Imagen 3",
                model: "imagen-3.0-generate-002",
                descriptionKey: LangKeys.ModelDescImagen3Generate002,
                task: ModelTaskCategory.Image,
                price: new ChatPriceUsage(),
                available: true
            ),

            /*
             * Local Ollama Models
             */

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "Deepseek R1 (14b)",
                model: "deepseek-r1:14b",
                task: ModelTaskCategory.Reasoning,
                dev: true
            ),

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.Other,
                type: ModelType.Chat,
                display: "Phi (2.7b)",
                model: "phi:latest",
                task: ModelTaskCategory.Other,
                dev: true
            ),

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.Microsoft,
                type: ModelType.Chat,
                display: "Phi 3 (3.8b)",
                model: "phi3:latest",
                task: ModelTaskCategory.Other,
                dev: true
            ),

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.MetaAI,
                type: ModelType.Chat,
                display: "LLaMA 3.2 (1b)",
                model: "llama3.2:1b",
                task: ModelTaskCategory.Other,
                dev: true
            ),

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.Other,
                type: ModelType.Chat,
                display: "TinyLLaMA (1.1b)",
                model: "tinyllama:latest",
                task: ModelTaskCategory.Other,
                dev: true
            ),

            new(
                request: ChatRequestType.Local,
                category: ModelCategory.DeepSeek,
                type: ModelType.Chat,
                display: "Deepseek R1 (7b)",
                model: "deepseek-r1:latest",
                task: ModelTaskCategory.Reasoning,
                dev: true
            ),
        };

        // Empty Message
        Console.WriteLine();

        OllamaApiClient? ollama = AresCore.OllamaClient;
        IEnumerable<Model> localModels = (ollama != null ? await ollama.ListLocalModelsAsync() : new List<Model>());

        foreach (ChatModel model in models)
        {
            if (ollama != null && model.RequestType == ChatRequestType.Local)
            {
                if (localModels.Any(m => m.Name.Equals(model.Model, StringComparison.OrdinalIgnoreCase)))
                    continue;

                await AresLogger.LogAsync($"Ollama: {model.Model}", "Ollama model not found, pulling it....\n");

                await foreach (var status in ollama.PullModelAsync(model.Model))
                {
                    if (status == null) continue;
                    await AresLogger.LogAsync($"Ollama: {model.Model}", $"{status.Percent}% {status.Status}");
                }
            }

            AresLogger.Log(
                $"AI: {model.Category.ToString()}",
                $"Type \"{model.Type.ToString().ToLower()}\" with model \"{model.Model.ToString().ToLower()}\" registered."
            );
        }

        // Empty Message
        Console.WriteLine();

        AresLogger.Log("AI", "Registered AI models.");

        return models;
    }

}