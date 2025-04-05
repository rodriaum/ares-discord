/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

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
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.Text;

namespace Ares.Core.Service;

public class AiService
{
    /// <summary>
    /// Dictionary mapping error keys to language keys for error messages
    /// </summary>
    private static readonly Dictionary<string, string> ErrorKeyMapping = new()
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

    /// <summary>
    /// Gets a localized error message based on the error key
    /// </summary>
    /// <param name="category">Language category</param>
    /// <param name="key">Error key</param>
    /// <returns>Localized error message</returns>
    private static string GetMessageByErrorKey(LangCategory category, string key)
    {
        LangManager manager = AresCore.LangManager;

        // Search for any matching error keys
        foreach (var mapping in ErrorKeyMapping)
        {
            if (key.Contains(mapping.Key))
            {
                return manager.GetTranslation(category, mapping.Value);
            }
        }

        // Default error message if no specific match is found
        return manager.GetTranslation(category, LangKeys.UnablePerformTask);
    }

    /*
     * General - Image Generation
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
    /// A string representing the URL of the generated image.
    /// </returns>
    /// <remarks>
    /// Future: Modify the function to return both the generated image URL and a boolean indicating whether the operation was successful.
    /// </remarks>
    public async static Task<string> GenerateImageUrlAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ImageGenOptions options,
        ulong channel,
        string prompt)
    {
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return errorMessage;
        }

        if (model.Type != ModelType.Image)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GTokenModel? tokenData = guild.Token;

        if (tokenData == null)
        {
            return guild.GetTranslation(LangKeys.CouldNotFindInfoToken);
        }

        string? openAiToken = tokenData.OpenAi;
        string? imgurToken = tokenData.Imgur;

        if (string.IsNullOrEmpty(openAiToken))
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        try
        {
            ImageGenerationOptions openAiOptions = options.ToImageGenerationOptions();
            GeneratedImage? image = await GenerateImageAsync(model, openAiToken, prompt, openAiOptions);

            if (image == null)
            {
                return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})";
            }

            // Use original URL if no Imgur token is available
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await WebUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            // Save the image generation to chat history
            if (!await SaveToHistoryAsync(guild, user, channel, prompt, imageUrl: imageUrl, imageOpenAi: image))
            {
                return guild.GetTranslation(LangKeys.CouldNotFindInfo) + $"({nameof(GenerateConversationAsync)})";
            }

            return imageUrl;
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate an image.", e.Message);

            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return GetMessageByErrorKey(lang, e.Message);
        }
    }

    /// <summary>
    /// Generates an image using the specified model
    /// </summary>
    private static async Task<GeneratedImage> GenerateImageAsync(
        ChatModel model,
        string token,
        string prompt,
        ImageGenerationOptions options)
    {
        OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

        ImageClient client = new ImageClient
        (
            model: model.Model,
            credential: new ApiKeyCredential(token),
            options: clientOptions
        );

        return await client.GenerateImageAsync(prompt, options);
    }

    /*
     * General - TTS Generation
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
    /// A tuple with a string representing the generated audio or error message, and a boolean indicating success.
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

        try
        {
            return await HandleOpenAiTTS(guild, user, model, prompt, tokenData);
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate TTS.", e.Message);

            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return (GetMessageByErrorKey(lang, e.Message), false);
        }
    }

    /// <summary>
    /// Handles text-to-speech generation using OpenAI
    /// </summary>
    private static async Task<(string, bool)> HandleOpenAiTTS(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        string prompt,
        GTokenModel tokenData)
    {
        string? token = tokenData.OpenAi;

        if (string.IsNullOrWhiteSpace(token))
            return (guild.GetTranslation(LangKeys.CouldNotFindToken), false);

        // Create client options
        OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = model.Category.GetEndpoint() };

        OpenAIClient client = new OpenAIClient
        (
            credential: new ApiKeyCredential(token),
            options: clientOptions
        );

        AudioClient ttsClient = client.GetAudioClient(model.Model);

        ClientResult<BinaryData> result = await ttsClient.GenerateSpeechAsync(prompt, GeneratedSpeechVoice.Alloy);

        return (result.Value.ToString(), true);
    }

    /*
     * General - Conversation Generation
     */

    /// <summary>
    /// Asynchronously generates a conversation based on the provided parameters.
    /// </summary>
    /// <param name="guild">Represents the guild (server) where the conversation is taking place. It contains information about the server.</param>
    /// <param name="user">Represents the user who is initiating the conversation. It contains information about the user.</param>
    /// <param name="model">Represents the chat model being used to generate the conversation. It defines the behavior and capabilities of the chat.</param>
    /// <param name="channel">The ID of the channel where the conversation is taking place.</param>
    /// <param name="prompt">The initial input or message that starts the conversation.</param>
    /// <param name="botMessage">An optional SocketMessage object to generate and update the message in real time, rather than waiting for completion.</param>
    /// <returns>
    /// A string representing the generated conversation text.
    /// </returns>
    public async static Task<string> GenerateConversationAsync(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        RestUserMessage? botMessage = null)
    {
        if (!ValidateParameters(guild, user, model, prompt, out string errorMessage))
        {
            return errorMessage;
        }

        if (model.Type != ModelType.Chat)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GTokenModel? tokenData = guild.Token;

        if (tokenData == null)
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        try
        {
            List<GChatHistoricModel>? historics = guild.ChatHistorics(user, channel: channel);
            return await HandleOpenAiConversation(guild, user, model, channel, prompt, tokenData, historics, botMessage);
        }
        catch (Exception e)
        {
            AresLogger.Error("Generation", "Unable to generate a conversation.", e.Message);

            LangCategory lang = guild.LangCategory() ?? AresCore.LangManager.GetLanguages().First();
            return GetMessageByErrorKey(lang, e.Message);
        }
    }

    /// <summary>
    /// Handles conversation generation using OpenAI
    /// </summary>
    private static async Task<string> HandleOpenAiConversation(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        GTokenModel tokenData,
        List<GChatHistoricModel>? historics,
        RestUserMessage? restBotMessage = null)
    {
        string? token = model.Category switch
        {
            ModelCategory.OpenAI => tokenData.OpenAi,
            ModelCategory.Anthropic => tokenData.Anthropic,
            ModelCategory.DeepSeek => tokenData.Deepseek,
            ModelCategory.xAI => tokenData.xAI,
            ModelCategory.Google => tokenData.Google,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(token))
            return guild.GetTranslation(LangKeys.CouldNotFindToken);

        // Prepare chat messages
        UserChatMessage userMessage = new UserChatMessage(prompt) { ParticipantName = user.GlobalName };

        List<ChatMessage> messages = GChatHistoricModel.ToChatOpenAiMessages(historics);
        messages.Add(userMessage);

        ModelCategory modelCategory = model.Category;

        // Create client options
        OpenAIClientOptions clientOptions = new OpenAIClientOptions { Endpoint = modelCategory.GetEndpoint() };

        // Create chat client
        ChatClient client = new ChatClient
            (
                model: model.Model,
                credential: new ApiKeyCredential(token),
                options: clientOptions
            );

        ChatCompletionOptions chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };

        // Use streaming or non-streaming based on whether we have a message to update
        if (restBotMessage != null && modelCategory.HasStreamingResponses())
        {
            return await HandleOpenAiStreamingResponse(guild, user, channel, prompt, model, restBotMessage, client, messages, chatOptions);
        }
        else
        {
            ChatCompletion completion = await client.CompleteChatAsync(messages, options: chatOptions);
            return await HandleOpenAiCompletionResponse(guild, user, channel, prompt, completion);
        }
    }

    /// <summary>
    /// Handles streaming response from OpenAI
    /// </summary>
    private static async Task<string> HandleOpenAiStreamingResponse(
        Guild guild,
        SocketGuildUser user,
        ulong channel,
        string prompt,
        ChatModel model,
        RestUserMessage restBotMessage,
        ChatClient client,
        List<ChatMessage> messages,
        ChatCompletionOptions options)
    {
        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            AresLogger.Error(nameof(HandleOpenAiStreamingResponse), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        // Create embed message
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle(guild.GetTranslation(LangKeys.AI))
            .WithColor(Color.Gold)
            .WithFooter(guild.GetTranslation(LangKeys.TakeUpMinutes));

        // Set cooldown to avoid updating too frequently
        DateTime lastEditDate = DateTime.UtcNow;
        TimeSpan editCooldownTime = TimeSpan.FromSeconds(1);

        StringBuilder sb = new StringBuilder();
        ChatTokenUsage? lastTokenUsage = null;

        // Process streaming response
        await foreach (StreamingChatCompletionUpdate response in client.CompleteChatStreamingAsync(messages, options))
        {
            if (response.ContentUpdate.Count > 0)
            {
                ChatMessageContentPart content = response.ContentUpdate[0];
                sb.Append(content.Text);
                string text = sb.ToString();

                // Limit text length for embed
                if (text.Length > 4096)
                {
                    text = text.Substring(0, 4095);
                    embed.WithFooter($"{DateTime.Now.Year} - Ares | {model.DisplayName} (Character limit reached)");
                }

                embed.WithDescription(text);

                // Update message if cooldown has passed
                if ((DateTime.UtcNow - lastEditDate) > editCooldownTime)
                {
                    await restBotMessage.ModifyAsync(message => message.Embed = embed.Build());
                    lastEditDate = DateTime.UtcNow;
                }
            }

            // Save the latest token usage information
            lastTokenUsage = response.Usage;
        }

        // Create usage stats
        ChatValueUsage usage = new ChatValueUsage();
        if (lastTokenUsage != null)
        {
            usage = new ChatValueUsage(lastTokenUsage.OutputTokenCount, lastTokenUsage.InputTokenCount);
        }

        // Save to history
        info.Historics.Add(new GChatHistoricModel(prompt, sb.ToString(), usage: usage));
        await guild.UpdateChatInfoAsync(user, info);

        return sb.ToString();
    }

    /// <summary>
    /// Handles non-streaming response from OpenAI
    /// </summary>
    private static async Task<string> HandleOpenAiCompletionResponse(
        Guild guild,
        SocketGuildUser user,
        ulong channel,
        string prompt,
        ChatCompletion? completion)
    {
        if (completion == null)
        {
            AresLogger.Error("OpenAI", "Unable to get response.", "");
            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleOpenAiConversation)})";
        }

        // Get chat info for this channel
        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel);
        if (info == null)
        {
            AresLogger.Error(nameof(HandleOpenAiConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        // Save to history
        GChatHistoricModel historic = GChatHistoricModel.From(prompt, responseOpenAi: completion)[0];
        info.Historics.Add(historic);
        await guild.UpdateChatInfoAsync(user, info);

        return ProcessOpenAiResponse(guild, completion);
    }

    /// <summary>
    /// Processes OpenAI response and handles different finish reasons
    /// </summary>
    private static string ProcessOpenAiResponse(Guild guild, ChatCompletion response)
    {
        ChatMessageContentPart? content = response.Content.FirstOrDefault();

        if (response == null || response.Content == null || content == null)
        {
            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleOpenAiConversation)})";
        }

        return response.FinishReason switch
        {
            ChatFinishReason.Stop => content.Text,
            ChatFinishReason.Length => guild.GetTranslation(LangKeys.RateLimitExceeded),
            ChatFinishReason.ContentFilter => guild.GetTranslation(LangKeys.ContentPolityViolation),
            ChatFinishReason.FunctionCall => guild.GetTranslation(LangKeys.FunctionCall),
            _ => guild.GetTranslation(LangKeys.UnableGenerateOrder).Replace("{0}", response.FinishReason.ToString())
        };
    }

    /// <summary>
    /// Validates input parameters and returns appropriate error message if invalid
    /// </summary>
    /// <returns>True if parameters are valid, false otherwise with error message</returns>
    private static bool ValidateParameters(Guild guild, IGuildUser user, ChatModel model, string prompt, out string errorMessage)
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
            historic = new GChatHistoricModel(prompt, response ?? string.Empty, usage: usage);
        }

        info.Historics.Add(historic);
        await guild.UpdateChatInfoAsync(user, info);

        return true;
    }
}