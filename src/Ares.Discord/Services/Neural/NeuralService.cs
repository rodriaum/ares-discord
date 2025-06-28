/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Chat.Image;
using Ares.Common.Models.Data;
using Ares.Common.Models.Data.Chat.Model;
using Ares.Common.Models.Language;
using Ares.Common.Models.Token;
using Ares.Common.Objects;
using Ares.Common.Util;
using Ares.Discord.Services.Api;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.Text.RegularExpressions;

namespace Ares.Discord.Service.Neural;

public class NeuralService
{
    private static GuildService? _guildService { get; set; }
    private static UserService? _userService { get; set; }

    public NeuralService()
    {
        _guildService = Program.GuildService;
        _userService = Program.UserService;

        if (_guildService == null || _userService == null)
        {
            AresLogger.Log(nameof(NeuralService), "Guild or User service is not initialized.", severity: Severity.Error);
            throw new InvalidOperationException("Guild or User service is not initialized.");
        }
    }

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
    public static async Task<(string, bool)> GenerateImageUrlAsync(
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
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.ModelUnavailable), false);
        }

        GToken? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindInfoToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());
        string? imgurToken = tokenData.GetToken("imgur");

        if (string.IsNullOrEmpty(modelToken))
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindToken), false);
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
                return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})", false);
            }

            // Use original URL if no Imgur token is available
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await WebUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            // Save the image generation to chat history
            if (!await SaveToHistoryAsync(guild, user, channel, prompt, imageUrl: imageUrl, imageOpenAi: image))
            {
                return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindInfo) + $" ({nameof(GenerateConversationAsync)})", false);
            }

            return (imageUrl, true);
        }
        catch (Exception e)
        {
            AresLogger.Log("Generation", "Unable to generate an image.", severity: Severity.Error, extra: e.Message);

            LanguageCategory lang = Program.LangManager.LanguageCategory(guild) ?? Program.LangManager.GetLanguages().First();
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
    public static async Task<(string, bool)> GenerateTTSAsync(
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
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.ModelUnavailable), false);
        }

        GToken? tokenData = guild.Token;

        if (tokenData == null)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindToken), false);
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
            AresLogger.Log("Generation", "Unable to generate TTS.", severity: Severity.Error, extra: e.Message);

            LanguageCategory lang = Program.LangManager.LanguageCategory(guild) ?? Program.LangManager.GetLanguages().First();
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
    public static async Task<(string, bool)> GenerateConversationAsync(
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

        if (model.Type != ModelType.Chat)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.ModelUnavailable), false);
        }

        try
        {
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
            AresLogger.Log("Generation", "Unable to generate a conversation.", severity: Severity.Error, extra: e.Message);
            LanguageCategory lang = Program.LangManager.LanguageCategory(guild) ?? Program.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    private static async Task<(string, bool)> HandleLocalModelRequestAsync(
        Guild guild,
        User user,
        ChatModel model,
        ulong channel,
        string prompt)
    {
        ApiResult<List<UserChatHistoric>>? historicsResult = await _userService!.GetChatHistory(user.Id, guild.Id, channelId: channel);
        List<UserChatHistoric>? historics = historicsResult != null && historicsResult.Success ? historicsResult.Data : null;

        ApiResult<UserChatInfo>? infoResult = await _userService.GetChatInfoByChannel(user.Id, guild.Id, channel);
        if (infoResult == null || !infoResult.Success || infoResult.Data == null)
        {
            AresLogger.Log(nameof(HandleLocalModelRequestAsync), "Chat information could not be accessed.", severity: Severity.Error);
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindInfo) + $" ({nameof(HandleLocalModelRequestAsync)})", false);
        }
        UserChatInfo info = infoResult.Data;

        List<Microsoft.Extensions.AI.ChatMessage> messages = historics != null ? UserChatHistoric.ToLocal(historics) : new List<Microsoft.Extensions.AI.ChatMessage>();
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, prompt));

        IChatClient? ollama = Program.OllamaClient;

        if (ollama == null)
        {
            AresLogger.Log(nameof(HandleLocalModelRequestAsync), "Ollama client is null.", severity: Severity.Error);
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.UnablePerformTask) + " (ollama_null)", false);
        }

        ChatOptions chatOptions = new ChatOptions
        {
            ModelId = model.Id,
            MaxOutputTokens = 2048
        };

        return await HandleLocalNonStreamingResponseAsync(guild, user, model, prompt, ollama, chatOptions, info, messages);
    }

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
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindToken), false);
        }

        string? modelToken = tokenData.GetToken(model.Category.ToString().ToLower());

        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindToken), false);
        }

        ApiResult<List<UserChatHistoric>>? historicsResult = await _userService!.GetChatHistory(user.Id, guild.Id, channelId: channel);
        List<UserChatHistoric>? historics = historicsResult != null && historicsResult.Success ? historicsResult.Data : null;

        ApiResult<UserChatInfo>? infoResult = await _userService!.GetChatInfoByChannel(user.Id, guild.Id, channel);
        if (infoResult == null || !infoResult.Success || infoResult.Data == null)
        {
            AresLogger.Log("GenerateConversationAsync", "Chat information could not be accessed.", severity: Severity.Error);
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindInfo) + $" ({nameof(HandleRemoteModelRequestAsync)})", false);
        }
        UserChatInfo info = infoResult.Data;

        List<OpenAI.Chat.ChatMessage> messages = historics != null ? UserChatHistoric.ToRemote(historics) : new List<OpenAI.Chat.ChatMessage>();
        messages.Add(new UserChatMessage(prompt));

        OpenAIClientOptions clientOptions = new OpenAIClientOptions
        {
            Endpoint = model.Category.GetEndpoint()
        };

        ChatClient client = new ChatClient(
            model: model.Id,
            credential: new ApiKeyCredential(modelToken),
            options: clientOptions
        );

        ChatCompletionOptions chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };

        return await HandleRemoteNonStreamingResponseAsync(guild, user, model, prompt, client, chatOptions, info, messages);
    }

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
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        UserChatHistoric historic = UserChatHistoric.From(prompt, responseOpenAi: completion)[0];

        info.Historics.Add(historic);

        await _userService!.UpdateChatInfo(user.Id, guild.Id, info);

        ChatMessageContentPart? content = completion.Content.FirstOrDefault();

        if (content == null)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $" {nameof(HandleRemoteNonStreamingResponseAsync)}", false);
        }

        string result = completion.FinishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => content.Text,
            OpenAI.Chat.ChatFinishReason.Length => Program.LangManager.GetTranslation(guild, LanguageKeys.RateLimitExceeded),
            OpenAI.Chat.ChatFinishReason.ContentFilter => Program.LangManager.GetTranslation(guild, LanguageKeys.ContentPolityViolation),
            OpenAI.Chat.ChatFinishReason.FunctionCall => Program.LangManager.GetTranslation(guild, LanguageKeys.FunctionCall),
            _ => Program.LangManager.GetTranslation(guild, LanguageKeys.UnableGenerateOrder).Replace("{0}", completion.FinishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

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
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        UserChatHistoric historic = UserChatHistoric.From(prompt, ollamaResponse: response)[0];

        info.Historics.Add(historic);

        await _userService!.UpdateChatInfo(user.Id, guild.Id, info);

        Microsoft.Extensions.AI.ChatMessage? message = response.Messages.FirstOrDefault();

        if (message == null)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $" {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        Microsoft.Extensions.AI.ChatFinishReason? finishReason = response.FinishReason;

        if (finishReason == null)
        {
            return (Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidRequest) + $"  {nameof(HandleLocalNonStreamingResponseAsync)}", false);
        }

        string pattern = @"<think>.*?</think>";
        string responseFixed = Regex.Replace(message.Text, pattern, string.Empty, RegexOptions.Singleline);

        string result = finishReason.ToString() switch
        {
            "stop" => responseFixed,
            "length" => Program.LangManager.GetTranslation(guild, LanguageKeys.RateLimitExceeded),
            "content_filter" => Program.LangManager.GetTranslation(guild, LanguageKeys.ContentPolityViolation),
            "tool_calls" => Program.LangManager.GetTranslation(guild, LanguageKeys.FunctionCall),
            _ => Program.LangManager.GetTranslation(guild, LanguageKeys.UnableGenerateOrder).Replace("{0}", finishReason.ToString() ?? "-/-")
        };

        return (result, true);
    }

    private static async Task<bool> SaveToHistoryAsync(
        Guild guild,
        User user,
        ulong channel,
        string prompt,
        List<string>? response = null,
        string? imageUrl = null,
        GeneratedImage? imageOpenAi = null,
        ChatCompletion? responseOpenAi = null,
        ChatTokenUsage? usage = null)
    {
        ApiResult<UserChatInfo>? infoResult = await _userService!.GetChatInfoByChannel(user.Id, guild.Id, channel);

        if (infoResult == null || !infoResult.Success || infoResult.Data == null)
        {
            AresLogger.Log(nameof(SaveToHistoryAsync), "It looks like the information could not be accessed.", severity: Severity.Error);
            return false;
        }

        UserChatInfo info = infoResult.Data;

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
            Common.Models.Chat.ChatTokenUsage? convertedUsage = (usage != null
                    ? new Common.Models.Chat.ChatTokenUsage(usage.OutputTokenCount, usage.InputTokenCount, usage.TotalTokenCount)
                    : new Common.Models.Chat.ChatTokenUsage());

            historic = new UserChatHistoric(prompt: prompt, response: response ?? new List<string>(), usage: convertedUsage);
        }

        info.Historics.Add(historic);
        await _userService!.UpdateChatInfo(user.Id, guild.Id, info);

        return true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a localized error message based on the error key
    /// </summary>
    /// <param name="category">Language category</param>
    /// <param name="key">Error key</param>
    /// <returns>Localized error message</returns>
    private static string GetMessageByErrorKey(LanguageCategory category, string key)
    {
        LanguageManager manager = Program.LangManager;

        // Dictionary mapping error keys to language keys for error messages
        Dictionary<string, string> errorKeyMapping = new()
        {
            { "content_policy_violation", LanguageKeys.ContentPolityViolation },
            { "rate_limit_exceeded", LanguageKeys.RateLimitExceeded },
            { "invalid_request", LanguageKeys.InvalidRequest },
            { "authentication_error", LanguageKeys.AuthenticationError },
            { "server_error", LanguageKeys.ServerError },
            { "overloaded_error", LanguageKeys.OverloadedError },
            { "timeout", LanguageKeys.Timeout },
            { "model_not_found", LanguageKeys.ModelNotFound }
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
        return manager.GetTranslation(category, LanguageKeys.UnablePerformTask);
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

    #endregion
}