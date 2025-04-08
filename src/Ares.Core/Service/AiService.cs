/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Core.Objects.Message;
using Ares.Ares.Core.Objects.Model;
using Ares.Ares.Core.Util;
using Ares.Core.Database.Model;
using Ares.Core.Database.Model.Chat.Sub;
using Ares.Core.Database.Model.Token;
using Ares.Core.Manager;
using Ares.Core.Objects.Chat;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Ares.Core.Service;

public class AiService
{
    /*
     * Image Generation
     */

    /// <summary>
    /// Asynchronously generates an image URL based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the image generation request is made. It contains information about the server.</param>
    /// <param name="user">Represents the user who is requesting the image generation. It contains information about the user.</param>
    /// <param name="model">Represents the chat model being used to generate the image. It defines the behavior and capabilities of the image generation process.</param>
    /// <param name="options">Represents the options for image generation, such as resolution, style, or other parameters.</param>
    /// <param name="channel">The ID of the channel where the image generation request is made.</param>
    /// <param name="prompt">The input or description used to generate the image.</param>
    /// <returns>
    /// A string representing the URL of the generated image, and a boolean indicating whether the image generation was successful.
    /// </returns>
    public async static Task<(string, bool)> GenerateImageUrlAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ImageGenOptions options,
        ulong channel,
        string prompt)
    {
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return (errorMessage, false);
        }

        if (model.Type != ModelType.Image)
        {
            return (guild.GetTranslation(LangKeys.ModelUnavailable), false);
        }

        GTokenModel? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindInfoToken), false);
        }

        string? modelToken = CoreUtil.GetTokenByModelCategory(model.Category, tokenData);
        string? imgurToken = tokenData.Imgur;

        if (string.IsNullOrEmpty(modelToken))
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);
        }

        try
        {
            ImageGenerationOptions generationOptions = options.ToImageGenerationOptions();
            OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

            ImageClient client = new ImageClient
            (
                model: model.Model,
                credential: new ApiKeyCredential(modelToken),
                options: clientOptions
            );

            // Generate the image using OpenAI API
            GeneratedImage? image = await client.GenerateImageAsync(prompt, generationOptions);

            if (image == null)
            {
                return (guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})", false);
            }

            // Use original URL if no Imgur token is available
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await WebUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            // Save the image generation to chat history
            if (!await SaveToHistoryAsync(guild, user, channel, prompt, imageUrl: imageUrl, imageOpenAi: image))
            {
                return (guild.GetTranslation(LangKeys.CouldNotFindInfo) + $"({nameof(GenerateConversationAsync)})", false);
            }

            return (imageUrl, true);
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate an image.", e.Message);

            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    /*
     * TTS Generation
     */

    /// <summary>
    /// Asynchronously generates a TTS based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the chat is taking place. It contains information about the server.</param>
    /// <param name="user">Represents the user who is initiating the chat. It contains information about the user.</param>
    /// <param name="model">Represents the chat model being used to generate the conversation. It defines the behavior and capabilities of the chat.</param>
    /// <param name="channel">The ID of the channel chat.</param>
    /// <param name="prompt">The initial input or message.</param>
    /// <returns>
    /// A tuple with a string representing the generated audio or error message, and a boolean indicating whether the image generation was successful.
    /// </returns>
    public async static Task<(string, bool)> GenerateTTSAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt)
    {
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return (errorMessage, false);
        }

        if (model.Type != ModelType.TTS)
        {
            return (guild.GetTranslation(LangKeys.ModelUnavailable), false);
        }

        GTokenModel? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);
        }

        string? modelToken = CoreUtil.GetTokenByModelCategory(model.Category, tokenData);

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);
        }

        OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

        OpenAIClient client = new OpenAIClient
        (
            credential: new ApiKeyCredential(modelToken),
            options: clientOptions
        );

        AudioClient ttsClient = client.GetAudioClient(model.Model);

        try
        {
            ClientResult<BinaryData> result = await ttsClient.GenerateSpeechAsync(prompt, GeneratedSpeechVoice.Alloy);

            return (result.Value.ToString(), true);
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate TTS.", e.Message);

            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    /*
     * Conversation Generation
     */

    /// <summary>
    /// Asynchronously generates a conversation based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the conversation is taking place.</param>
    /// <param name="user">Represents the user who is initiating the conversation.</param>
    /// <param name="model">Represents the chat model being used to generate the conversation.</param>
    /// <param name="channel">The ID of the channel where the conversation is taking place.</param>
    /// <param name="prompt">The initial input or message that starts the conversation.</param>
    /// <param name="botMessage">An optional message object to update in real time with the response.</param>
    /// <returns>
    /// A tuple containing the generated conversation text and a boolean indicating success.
    /// </returns>
    public async static Task<(string, bool)> GenerateConversationAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        RestUserMessage? botMessage = null)
    {
        // Validate input parameters first
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return (errorMessage, false);
        }

        if (model.Type != ModelType.Chat)
        {
            return (guild.GetTranslation(LangKeys.ModelUnavailable), false);
        }

        try
        {
            // Handle different model request types
            if (model.RequestType == ChatRequestType.Local)
            {
                return await HandleLocalModelRequestAsync(guild, user, model, channel, prompt, botMessage);
            }
            else
            {
                return await HandleRemoteModelRequestAsync(guild, user, model, channel, prompt, botMessage);
            }
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate a conversation.", e.Message);
            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    /// <summary>
    /// Handles conversation generation using local models via Ollama.
    /// </summary>
    private static async Task<(string, bool)> HandleLocalModelRequestAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        RestUserMessage? botMessage)
    {
        // Get chat history and info
        List<GChatHistoricModel>? historics = guild.ChatHistorics(user, channel: channel);

        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(HandleLocalModelRequestAsync), "Chat information could not be accessed.");
            return (guild.GetTranslation(LangKeys.CouldNotFindInfo), false);
        }

        // Prepare messages for the API request
        List<Microsoft.Extensions.AI.ChatMessage> messages = historics != null ? GChatHistoricModel.ToLocal(historics) : new();

        Microsoft.Extensions.AI.ChatMessage userMessage = new(Microsoft.Extensions.AI.ChatRole.User, prompt);
        userMessage.AuthorName = user.GlobalName;

        messages.Add(userMessage);

        // Configure the API client
        IChatClient? ollama = AresCore.OllamaClient;

        if (ollama == null)
        {
            AresLogger.Error(nameof(HandleLocalModelRequestAsync), "Ollama client is null.");
            return (guild.GetTranslation(LangKeys.UnablePerformTask), false);
        }

        ChatOptions chatOptions = new ChatOptions
        {
            ModelId = model.Model,
            MaxOutputTokens = 2048
        };

        // Use appropriate method based on streaming capability
        if (botMessage != null && model.Category.HasLocalStreamingResponses())
        {
            return await HandleLocalStreamingResponseAsync(guild, user, model, prompt, botMessage, ollama, chatOptions, info, messages);
        }
        else
        {
            return await HandleLocalNonStreamingResponseAsync(guild, user, model, prompt, ollama, chatOptions, info, messages);
        }
    }

    /// <summary>
    /// Handles conversation generation using remote API models.
    /// </summary>
    private static async Task<(string, bool)> HandleRemoteModelRequestAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        RestUserMessage? botMessage)
    {
        GTokenModel? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);
        }

        string? modelToken = CoreUtil.GetTokenByModelCategory(model.Category, tokenData);

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);
        }

        // Get chat history and info
        List<GChatHistoricModel>? historics = guild.ChatHistorics(user, channel: channel);
        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error("GenerateConversationAsync", "Chat information could not be accessed.");
            return (guild.GetTranslation(LangKeys.CouldNotFindInfo), false);
        }

        // Prepare messages for the API request
        List<OpenAI.Chat.ChatMessage> messages = historics != null ? GChatHistoricModel.ToRemote(historics) : new();

        UserChatMessage userMessage = new(prompt) { ParticipantName = user.GlobalName };
        messages.Add(userMessage);

        // Configure the API client
        OpenAIClientOptions clientOptions = new OpenAIClientOptions
        {
            Endpoint = model.Category.GetEndpoint()
        };

        ChatClient client = new(
            model: model.Model,
            credential: new(modelToken),
            options: clientOptions
        );

        ChatCompletionOptions chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };

        // Use appropriate method based on streaming capability
        if (botMessage != null && model.Category.HasRemoteStreamingResponses())
        {
            return await HandleRemoteStreamingResponseAsync(guild, user, model, prompt, botMessage, client, chatOptions, info, messages);
        }
        else
        {
            return await HandleRemoteNonStreamingResponseAsync(guild, user, model, prompt, client, chatOptions, info, messages);
        }
    }

    /// <summary>
    /// Handles streaming API responses with real-time updates.
    /// </summary>
    private static async Task<(string, bool)> HandleRemoteStreamingResponseAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        string prompt,
        RestUserMessage botMessage,
        ChatClient client,
        ChatCompletionOptions chatOptions,
        GChatInfoModel info,
        List<OpenAI.Chat.ChatMessage> messages)
    {
        StringBuilder responseBuilder = new();
        EmbedBuilder embed = CreateResponseEmbed(guild);

        ChatTokenUsage? lastTokenUsage = null;
        MessageUpdater messageUpdater = new(botMessage, embed, TimeSpan.FromSeconds(1));

        await foreach (StreamingChatCompletionUpdate response in client.CompleteChatStreamingAsync(messages, chatOptions))
        {
            if (response.ContentUpdate.Count > 0)
            {
                ChatMessageContentPart content = response.ContentUpdate[0];
                responseBuilder.Append(content.Text);

                await messageUpdater.UpdateMessageAsync(responseBuilder.ToString());
            }

            // Save the latest token usage information
            lastTokenUsage = response.Usage;
        }

        // Save to history
        ChatValueUsage usage = lastTokenUsage != null
            ? new(lastTokenUsage.OutputTokenCount, lastTokenUsage.InputTokenCount)
            : new();

        info.Historics.Add(new GChatHistoricModel(prompt: prompt, response: responseBuilder.ToString(), usage: usage));
        await guild.UpdateChatInfoAsync(user, info);

        return (responseBuilder.ToString(), true);
    }

    /// <summary>
    /// Handles non-streaming API responses.
    /// </summary>
    private static async Task<(string, bool)> HandleRemoteNonStreamingResponseAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        string prompt,
        ChatClient client,
        ChatCompletionOptions chatOptions,
        GChatInfoModel info,
        List<OpenAI.Chat.ChatMessage> messages)
    {
        ChatCompletion completion = await client.CompleteChatAsync(messages, options: chatOptions);

        if (completion == null)
        {
            AresLogger.Error("OpenAI", "Unable to get response.");
            return (guild.GetTranslation(LangKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        // Save to history
        GChatHistoricModel historic = GChatHistoricModel.From(prompt, responseOpenAi: completion)[0];

        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        ChatMessageContentPart? content = completion.Content.FirstOrDefault();

        if (content == null)
        {
            return (guild.GetTranslation(LangKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        string result = completion.FinishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => content.Text,
            OpenAI.Chat.ChatFinishReason.Length => guild.GetTranslation(LangKeys.RateLimitExceeded),
            OpenAI.Chat.ChatFinishReason.ContentFilter => guild.GetTranslation(LangKeys.ContentPolityViolation),
            OpenAI.Chat.ChatFinishReason.FunctionCall => guild.GetTranslation(LangKeys.FunctionCall),
            _ => guild.GetTranslation(LangKeys.UnableGenerateOrder).Replace("{0}", completion.FinishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

    /// <summary>
    /// Handles streaming API responses with real-time updates.
    /// </summary>
    private static async Task<(string, bool)> HandleLocalStreamingResponseAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        string prompt,
        RestUserMessage botMessage,
        IChatClient client,
        ChatOptions chatOptions,
        GChatInfoModel info,
        List<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        StringBuilder responseBuilder = new();
        EmbedBuilder embed = CreateResponseEmbed(guild);

        ChatTokenUsage? lastTokenUsage = null;
        MessageUpdater messageUpdater = new(botMessage, embed, TimeSpan.FromSeconds(1));

        await foreach (var response in client.GetStreamingResponseAsync(messages, chatOptions))
        {
            if (response.Contents.Count > 0)
            {
                string content = response.Text;
                responseBuilder.Append(content);
                await messageUpdater.UpdateMessageAsync(responseBuilder.ToString());
            }

            // Save the latest token usage information
            // lastTokenUsage = response.Usage;
        }

        // Save to history
        ChatValueUsage usage = lastTokenUsage != null
            ? new(lastTokenUsage.OutputTokenCount, lastTokenUsage.InputTokenCount)
            : new();

        info.Historics.Add(new GChatHistoricModel(prompt: prompt, response: responseBuilder.ToString(), usage: usage));
        await guild.UpdateChatInfoAsync(user, info);

        // Regular expression to remove everything between <think> and </think>
        string pattern = @"<think>.*?</think>";
        string responseFixed = Regex.Replace(responseBuilder.ToString(), pattern, string.Empty, RegexOptions.Singleline);

        return (responseFixed, true);
    }

    /// <summary>
    /// Handles non-streaming API responses.
    /// </summary>
    private static async Task<(string, bool)> HandleLocalNonStreamingResponseAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        string prompt,
        IChatClient client,
        ChatOptions chatOptions,
        GChatInfoModel info,
        List<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        ChatResponse response = await client.GetResponseAsync(messages, options: chatOptions);

        if (response == null)
        {
            AresLogger.Error("OpenAI", "Unable to get response.");
            return (guild.GetTranslation(LangKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        // Save to history
        GChatHistoricModel historic = GChatHistoricModel.From(prompt, ollamaResponse: response)[0];

        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        Microsoft.Extensions.AI.ChatMessage? message = response.Messages.FirstOrDefault();

        if (message == null)
        {
            return (guild.GetTranslation(LangKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        Microsoft.Extensions.AI.ChatFinishReason? finishReason = response.FinishReason;

        if (finishReason == null)
        {
            return (guild.GetTranslation(LangKeys.InvalidRequest) + $"  {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        // Regular expression to remove everything between <think> and </think>
        string pattern = @"<think>.*?</think>";
        string responseFixed = Regex.Replace(message.Text, pattern, string.Empty, RegexOptions.Singleline);

        string result = finishReason.ToString() switch
        {
            "stop" => responseFixed,
            "length" => guild.GetTranslation(LangKeys.RateLimitExceeded),
            "content_filter" => guild.GetTranslation(LangKeys.ContentPolityViolation),
            "tool_calls" => guild.GetTranslation(LangKeys.FunctionCall),
            _ => guild.GetTranslation(LangKeys.UnableGenerateOrder).Replace("{0}", finishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

    /// <summary>
    /// Creates a formatted embed for response messages.
    /// </summary>
    private static EmbedBuilder CreateResponseEmbed(Guild guild)
    {
        return new EmbedBuilder()
            .WithTitle(guild.GetTranslation(LangKeys.AI))
            .WithColor(Color.Gold)
            .WithFooter(guild.GetTranslation(LangKeys.TakeUpMinutes));
    }

    /*
     * Helper Methods
     */

    /// <summary>
    /// Gets a localized error message based on the error key
    /// </summary>
    /// <param name="category">Language category</param>
    /// <param name="key">Error key</param>
    /// <returns>Localized error message</returns>
    private static string GetMessageByErrorKey(LangCategory category, string key)
    {
        LangManager manager = AresCore.LangManager;

        // Dictionary mapping error keys to language keys for error messages
        Dictionary<string, string> errorKeyMapping = new()
    {
        { "content_policy_violation", LangKeys.ContentPolityViolation },
        { "rate_limit_exceeded", LangKeys.RateLimitExceeded },
        { "invalid_request", LangKeys.InvalidRequest },
        { "authentication_error", LangKeys.AuthenticationError },
        { "server_error", LangKeys.ServerError },
        { "overloaded_error", LangKeys.OverloadedError },
        { "timeout", LangKeys.Timeout },
        { "model_not_found", LangKeys.ModelNotFound }
    };

        // Search for any matching error keys
        foreach (var mapping in errorKeyMapping)
        {
            if (key.Contains(mapping.Key))
            {
                return manager.GetTranslation(category, mapping.Value);
            }
        }

        // Default error message if no specific match is found
        return manager.GetTranslation(category, LangKeys.UnablePerformTask);
    }

    /// <summary>
    /// Validates input parameters and returns appropriate error message if invalid
    /// </summary>
    /// <returns>True if parameters are valid, false otherwise with error message</returns>
    private static bool ValidateParameters(
        Guild guild,
        IGuildUser user,
        ChatModel model,
        string prompt,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        // Parameter validation
        if (guild == null)
        {
            errorMessage = "There was an internal issue identifying the guild. Please check if the guild was provided correctly.";
            return false;
        }

        if (user == null)
        {
            errorMessage = "There was an internal issue identifying the user. Please check if the user was provided correctly.";
            return false;
        }

        if (model == null)
        {
            errorMessage = "There was an internal issue identifying the model. Please check if the model was provided correctly.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            errorMessage = "There was an internal issue identifying the prompt. Please check if the prompt was provided correctly.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Helper method to save chat or image response to history
    /// </summary>
    private static async Task<bool> SaveToHistoryAsync(
        Guild guild,
        SocketGuildUser user,
        ulong channel,
        string prompt,
        string? response = null,
        string? imageUrl = null,
        GeneratedImage? imageOpenAi = null,
        ChatCompletion? responseOpenAi = null,
        ChatValueUsage? usage = null)
    {
        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(SaveToHistoryAsync), "It looks like the information could not be accessed.");
            return false;
        }

        GChatHistoricModel historic;

        if (imageOpenAi != null)
        {
            historic = GChatHistoricModel.From(prompt, imageUrl: imageUrl, imageOpenAi: imageOpenAi)[0];
        }
        else if (responseOpenAi != null)
        {
            historic = GChatHistoricModel.From(prompt, responseOpenAi: responseOpenAi)[0];
        }
        else
        {
            historic = new GChatHistoricModel(prompt: prompt, response: response ?? string.Empty, usage: usage);
        }

        info.Historics.Add(historic);
        await guild.UpdateChatInfoAsync(user, info);

        return true;
    }
}