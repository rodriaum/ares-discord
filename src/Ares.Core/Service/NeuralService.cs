/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Manager.Database;
using Ares.Core.Manager.Lang;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Collection;
using Ares.Core.Models.Token;
using Ares.Core.Objects;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.Text.RegularExpressions;

namespace Ares.Core.Service;

public class NeuralService
{

    #region Image Generation

    /// <summary>
    /// Asynchronously generates an image URL based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the image generation request is made. It contains information about the server.</param>
    /// <param name="userId">Represents the user who is requesting the image generation. It contains information about the user.</param>
    /// <param name="model">Represents the chat model being used to generate the image. It defines the behavior and capabilities of the image generation process.</param>
    /// <param name="options">Represents the options for image generation, such as resolution, style, or other parameters.</param>
    /// <param name="channel">The ID of the channel where the image generation request is made.</param>
    /// <param name="prompt">The input or description used to generate the image.</param>
    /// <returns>
    /// A string representing the URL of the generated image, and a boolean indicating whether the image generation was successful.
    /// </returns>
    public async static Task<(string, bool)> GenerateImageUrlAsync(
        Guild guild,
        User user,
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
            return (GuildManager.GetTranslation(guild, LangKeys.ModelUnavailable), false);
        }

        GToken? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindInfoToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());
        string? imgurToken = tokenData.GetToken("imgur");

        if (string.IsNullOrEmpty(modelToken))
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindToken), false);
        }

        try
        {
            ImageGenerationOptions generationOptions = options.ToImageGenerationOptions();
            OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

            ImageClient client = new ImageClient
            (
                model: model.Id,
                credential: new ApiKeyCredential(modelToken),
                options: clientOptions
            );

            // Generate the image using OpenAI API
            GeneratedImage? image = await client.GenerateImageAsync(prompt, generationOptions);

            if (image == null)
            {
                return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})", false);
            }

            // Use original URL if no Imgur token is available
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await WebUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            // Save the image generation to chat history
            if (!await SaveToHistoryAsync(guild, user, channel, prompt, imageUrl: imageUrl, imageOpenAi: image))
            {
                return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindInfo) + $"({nameof(GenerateConversationAsync)})", false);
            }

            return (imageUrl, true);
        }
        catch (Exception e)
        {
            AresLogger.Log("Generation", "Unable to generate an image.", e.Message, severity: Severity.Error);

            LangCategory lang = GuildManager.LangCategory(guild) ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    #endregion

    #region TTS Generation

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
        User user,
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
            return (GuildManager.GetTranslation(guild, LangKeys.ModelUnavailable), false);
        }

        GToken? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindToken), false);
        }

        OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

        OpenAIClient client = new OpenAIClient
        (
            credential: new ApiKeyCredential(modelToken),
            options: clientOptions
        );

        AudioClient ttsClient = client.GetAudioClient(model.Id);

        try
        {
            ClientResult<BinaryData> result = await ttsClient.GenerateSpeechAsync(prompt, GeneratedSpeechVoice.Alloy);

            return (result.Value.ToString(), true);
        }
        catch (Exception e)
        {
            AresLogger.Log("Generation", "Unable to generate TTS.", e.Message, severity: Severity.Error);

            LangCategory lang = GuildManager.LangCategory(guild) ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    #endregion

    #region Conversation Generation

    /// <summary>
    /// Asynchronously generates a conversation based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the conversation is taking place.</param>
    /// <param name="user">Represents the user who is initiating the conversation.</param>
    /// <param name="model">Represents the chat model being used to generate the conversation.</param>
    /// <param name="channel">The ID of the channel where the conversation is taking place.</param>
    /// <param name="prompt">The initial input or message that starts the conversation.</param>
    /// <returns>
    /// A tuple containing the generated conversation text and a boolean indicating success.
    /// </returns>
    public async static Task<(string, bool)> GenerateConversationAsync(
        Guild guild,
        User user,
        ChatModel model,
        ulong channel,
        string prompt)
    {
        // Validate input parameters first
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return (errorMessage, false);
        }

        if (model.Type != ModelType.Chat)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.ModelUnavailable), false);
        }

        try
        {
            // Handle different model request types
            if (model.RequestType == ChatRequestType.Local)
            {
                return await HandleLocalModelRequestAsync(guild, user, model, channel, prompt);
            }
            else
            {
                return await HandleRemoteModelRequestAsync(guild, user, model, channel, prompt);
            }
        }
        catch (Exception e)
        {
            AresLogger.Log("Generation", "Unable to generate a conversation.", e.Message, severity: Severity.Error);
            LangCategory lang = GuildManager.LangCategory(guild) ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    /// <summary>
    /// Handles conversation generation using local models via Ollama.
    /// </summary>
    private static async Task<(string, bool)> HandleLocalModelRequestAsync(
        Guild guild,
        User user,
        ChatModel model,
        ulong channel,
        string prompt)
    {
        // Get chat history and info
        List<UserChatHistoric>? historics = UserManager.ChatHistorics(user, guild.Id, channelId: channel);

        UserChatInfo? info = UserManager.ChatInfoByChannel(user, guild.Id, channel);

        if (info == null)
        {
            AresLogger.Log(nameof(HandleLocalModelRequestAsync), "Chat information could not be accessed.", severity: Severity.Error);
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindInfo), false);
        }

        // Prepare messages for the API request
        List<Microsoft.Extensions.AI.ChatMessage> messages = historics != null ? UserChatHistoric.ToLocal(historics) : new();
        messages.Add(new(ChatRole.User, prompt));

        // Configure the API client
        IChatClient? ollama = AresCore.OllamaClient;

        if (ollama == null)
        {
            AresLogger.Log(nameof(HandleLocalModelRequestAsync), "Ollama client is null.", severity: Severity.Error);
            return (GuildManager.GetTranslation(guild, LangKeys.UnablePerformTask) + " (ollama_null)", false);
        }

        ChatOptions chatOptions = new ChatOptions
        {
            ModelId = model.Id,
            MaxOutputTokens = 2048
        };

        return await HandleLocalNonStreamingResponseAsync(guild, user, model, prompt, ollama, chatOptions, info, messages);
    }

    /// <summary>
    /// Handles conversation generation using remote API models.
    /// </summary>
    private static async Task<(string, bool)> HandleRemoteModelRequestAsync(
        Guild guild,
        User user,
        ChatModel model,
        ulong channel,
        string prompt)
    {
        GToken? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindToken), false);
        }

        // Get chat history and info
        List<UserChatHistoric>? historics = UserManager.ChatHistorics(user, guild.Id, channelId: channel);
        UserChatInfo? info = UserManager.ChatInfoByChannel(user, guild.Id, channel);

        if (info == null)
        {
            AresLogger.Log("GenerateConversationAsync", "Chat information could not be accessed.", severity: Severity.Error);
            return (GuildManager.GetTranslation(guild, LangKeys.CouldNotFindInfo), false);
        }

        // Prepare messages for the API request
        List<OpenAI.Chat.ChatMessage> messages = historics != null ? UserChatHistoric.ToRemote(historics) : new();
        messages.Add(new UserChatMessage(prompt));

        // Configure the API client
        OpenAIClientOptions clientOptions = new OpenAIClientOptions
        {
            Endpoint = model.Category.GetEndpoint()
        };

        ChatClient client = new(
            model: model.Id,
            credential: new(modelToken),
            options: clientOptions
        );

        ChatCompletionOptions chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };

        return await HandleRemoteNonStreamingResponseAsync(guild, user, model, prompt, client, chatOptions, info, messages);
    }

    /// <summary>
    /// Handles non-streaming API responses.
    /// </summary>
    private static async Task<(string, bool)> HandleRemoteNonStreamingResponseAsync(
        Guild guild,
        User user,
        ChatModel model,
        string prompt,
        ChatClient client,
        ChatCompletionOptions chatOptions,
        UserChatInfo info,
        List<OpenAI.Chat.ChatMessage> messages)
    {
        ChatCompletion completion = await client.CompleteChatAsync(messages, options: chatOptions);

        if (completion == null)
        {
            AresLogger.Log("OpenAI", "Unable to get response.", severity: Severity.Error);
            return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        // Save to history
        UserChatHistoric historic = UserChatHistoric.From(prompt, responseOpenAi: completion)[0];

        info.Historics.Add(historic);

        await UserManager.UpdateChatInfoAsync(user, guild.Id, info);

        ChatMessageContentPart? content = completion.Content.FirstOrDefault();

        if (content == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        string result = completion.FinishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => content.Text,
            OpenAI.Chat.ChatFinishReason.Length => GuildManager.GetTranslation(guild, LangKeys.RateLimitExceeded),
            OpenAI.Chat.ChatFinishReason.ContentFilter => GuildManager.GetTranslation(guild, LangKeys.ContentPolityViolation),
            OpenAI.Chat.ChatFinishReason.FunctionCall => GuildManager.GetTranslation(guild, LangKeys.FunctionCall),
            _ => GuildManager.GetTranslation(guild, LangKeys.UnableGenerateOrder).Replace("{0}", completion.FinishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

    /// <summary>
    /// Handles non-streaming API responses.
    /// </summary>
    private static async Task<(string, bool)> HandleLocalNonStreamingResponseAsync(
        Guild guild,
        User user,
        ChatModel model,
        string prompt,
        IChatClient client,
        ChatOptions chatOptions,
        UserChatInfo info,
        List<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        ChatResponse response = await client.GetResponseAsync(messages, options: chatOptions);

        if (response == null)
        {
            AresLogger.Log("OpenAI", "Unable to get response.", severity: Severity.Error);
            return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        // Save to history
        UserChatHistoric historic = UserChatHistoric.From(prompt, ollamaResponse: response)[0];

        info.Historics.Add(historic);

        await UserManager.UpdateChatInfoAsync(user, guild.Id, info);

        Microsoft.Extensions.AI.ChatMessage? message = response.Messages.FirstOrDefault();

        if (message == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        Microsoft.Extensions.AI.ChatFinishReason? finishReason = response.FinishReason;

        if (finishReason == null)
        {
            return (GuildManager.GetTranslation(guild, LangKeys.InvalidRequest) + $"  {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        // Regular expression to remove everything between <think> and </think>
        string pattern = @"<think>.*?</think>";
        string responseFixed = Regex.Replace(message.Text, pattern, string.Empty, RegexOptions.Singleline);

        string result = finishReason.ToString() switch
        {
            "stop" => responseFixed,
            "length" => GuildManager.GetTranslation(guild, LangKeys.RateLimitExceeded),
            "content_filter" => GuildManager.GetTranslation(guild, LangKeys.ContentPolityViolation),
            "tool_calls" => GuildManager.GetTranslation(guild, LangKeys.FunctionCall),
            _ => GuildManager.GetTranslation(guild, LangKeys.UnableGenerateOrder).Replace("{0}", finishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

    #endregion

    #region Helper Methods

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
        User user,
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
        User user,
        ulong channel,
        string prompt,
        string? response = null,
        string? imageUrl = null,
        GeneratedImage? imageOpenAi = null,
        ChatCompletion? responseOpenAi = null,
        Objects.Chat.ChatTokenUsage? usage = null)
    {
        UserChatInfo? info = UserManager.ChatInfoByChannel(user, guild.Id, channel);

        if (info == null)
        {
            AresLogger.Log(nameof(SaveToHistoryAsync), "It looks like the information could not be accessed.", severity: Severity.Error);
            return false;
        }

        UserChatHistoric historic;

        if (imageOpenAi != null)
        {
            historic = UserChatHistoric.From(prompt, imageUrl: imageUrl, imageOpenAi: imageOpenAi)[0];
        }
        else if (responseOpenAi != null)
        {
            historic = UserChatHistoric.From(prompt, responseOpenAi: responseOpenAi)[0];
        }
        else
        {
            historic = new UserChatHistoric(prompt: prompt, response: response ?? string.Empty, usage: usage);
        }

        info.Historics.Add(historic);
        await UserManager.UpdateChatInfoAsync(user, guild.Id, info);

        return true;
    }

    #endregion
}