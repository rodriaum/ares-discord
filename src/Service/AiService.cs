using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Ares.src.Backend.Data.Model;
using Ares.src.Backend.Data.Model.Chat.Sub;
using Ares.src.Backend.Data.Model.Information;
using Ares.src.Backend.Data.Model.Token;
using Ares.src.Manager;
using Ares.src.Objects.Chat.Image;
using Ares.src.Objects.Language;
using Ares.src.Objects.Model;
using Ares.src.Utils.Extra;
using DeepSeek.Core;
using DeepSeek.Core.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using OpenAI.Chat;
using OpenAI.Images;
using System.Text;

namespace Ares.src.Service;

public class AiService
{
    private static string GetMessageByErrorKey(LangCategory category, string key)
    {
        LangManager manager = Core.LangManager;

        if (key.Contains("content_policy_violation"))
        {
            return manager.GetTranslation(category, LangKeys.ContentPolityViolation);
        }
        else if (key.Contains("rate_limit_exceeded"))
        {
            return manager.GetTranslation(category, LangKeys.RateLimitExceeded);
        }
        else if (key.Contains("invalid_request"))
        {
            return manager.GetTranslation(category, LangKeys.InvalidRequest);
        }
        else if (key.Contains("authentication_error"))
        {
            return manager.GetTranslation(category, LangKeys.AuthenticationError);
        }
        else if (key.Contains("server_error"))
        {
            return manager.GetTranslation(category, LangKeys.ServerError);
        }
        else if (key.Contains("timeout"))
        {
            return manager.GetTranslation(category, LangKeys.Timeout);
        }
        else if (key.Contains("model_not_found"))
        {
            return manager.GetTranslation(category, LangKeys.ModelNotFound);
        }
        else
        {
            return manager.GetTranslation(category, LangKeys.UnablePerformTask);
        }
    }

    /// <summary>
    /// <b>General</b> - Image Generation
    /// </summary>
    /// 

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
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Image)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GInformationModel information = guild.Information;
        GTokenModel? tokenData = information.Token;

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
            ImageGenerationOptions openAiOptions = AiUtil.GetImageGenerationOptions(options);
            GeneratedImage? image = await GenerateImageAsync(model, openAiToken, prompt, openAiOptions);

            if (image == null)
            {
                return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})";
            }

            // Usa a URL original se não houver um token Imgur.
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await RequestUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            ChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

            if (info == null)
            {
                LogUtil.Error(nameof(GenerateImageUrlAsync), "It looks like the information could not be accessed.");
                return guild.GetTranslation(LangKeys.CouldNotFindInfo) + $"({nameof(GenerateConversationAsync)})";
            }

            ChatHistoricModel historic = AiUtil.ConvertToChatHistoric(prompt, imageUrl: imageUrl, imageOpenAi: image)[0];
            info.Historics.Add(historic);

            await guild.UpdateChatInfoAsync(user, info);

            return imageUrl;
        }
        catch (Exception e)
        {
            LogUtil.Error("Generation", "Unable to generate an image.", e.Message);
            return GetMessageByErrorKey(guild.LanguageCategory(), e.Message);
        }
    }

    private static async Task<GeneratedImage> GenerateImageAsync(
        ChatModel model,
        string token,
        string prompt,
        ImageGenerationOptions options)
    {
        ImageClient client = new ImageClient(model.Model, token);

        return await client.GenerateImageAsync(prompt, options);
    }

    /// <summary>
    /// <b>General</b> - Conversation Generation
    /// </summary>

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
    /// <remarks>
    /// Future: Modify the function to return both the generated text and a boolean indicating whether the operation was successful.
    /// </remarks>
    public async static Task<string> GenerateConversationAsync(
        Guild guild, 
        SocketGuildUser user, 
        ChatModel model,
        ulong channel, 
        string prompt, 
        RestUserMessage? botMessage = null) 
    {
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Chat)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GInformationModel information = guild.Information;
        GTokenModel? tokenData = information.Token;

        if (tokenData == null)
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        ChatHistoricModel? historic = null;

        try
        {
            List<ChatHistoricModel>? historics = guild.ChatHistorics(user, channel: channel);

            switch (model.Category)
            {
                case ModelCategory.OpenAI:
                    return await HandleOpenAiConversation(guild, user, model, channel, prompt, tokenData, historics, botMessage);

                case ModelCategory.Anthropic:
                    return await HandleAnthropicConversation(guild, user, model, channel, prompt, tokenData, historics, botMessage);

                case ModelCategory.DeepSeek:
                    return await HandleDeepSeekConversation(guild, user, model, channel, prompt, tokenData, historics, botMessage);

                default:
                    return guild.GetTranslation(LangKeys.ModelNotFound);
            }
        }
        catch (Exception e)
        {
            if (user != null && channel != 0 && historic != null && !await guild.RemoveConversationAsync(user, channel, historic))
            {
                LogUtil.Error("Generation", "Unable to remove user conversation after internal issue" );
            }

            LogUtil.Error("Generation", "Unable to generate an conversation.", e.Message);
            return GetMessageByErrorKey(guild.LanguageCategory(), e.Message);
        }
    }

    /// <summary>
    /// <b>OpenAI</b> - Conversation Generation
    /// </summary>

    private static async Task<string> HandleOpenAiConversation(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channel,
        string prompt,
        GTokenModel tokenData,
        List<ChatHistoricModel>? historics,
        RestUserMessage? restBotMessage = null)
    {
        string? token = tokenData.OpenAi;

        if (string.IsNullOrWhiteSpace(token))
            return guild.GetTranslation(LangKeys.CouldNotFindToken);

        UserChatMessage userMessage = new UserChatMessage(prompt) { ParticipantName = user.GlobalName };
        List<ChatMessage> messages = AiUtil.GetChatOpenAiMessages(historics);

        messages.Add(userMessage);

        ChatClient client = new ChatClient(model.Model, token);
        ChatCompletionOptions options = new ChatCompletionOptions { MaxOutputTokenCount = 2048 };

        if (restBotMessage != null)
        {
            return await HandleOpenAiStreamingResponse(guild, user, channel, prompt, model, restBotMessage, client, messages, options);
        }
        else
        {
            ChatCompletion completion = await client.CompleteChatAsync(messages, options: options);
            return await HandleOpenAiCompletionResponse(guild, user, channel, prompt, completion);
        }
    }

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
        ChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAiStreamingResponse), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle(guild.GetTranslation(LangKeys.AI))
            .WithColor(Color.Gold)
            .WithFooter(guild.GetTranslation(LangKeys.TakeUpMinutes));

        DateTime lastEditDate = DateTime.UtcNow;
        TimeSpan editCooldownTime = TimeSpan.FromSeconds(1);

        StringBuilder sb = new StringBuilder();

        await foreach (var response in client.CompleteChatStreamingAsync(messages, options))
        {
            if (response.ContentUpdate.Count > 0)
            {
                ChatMessageContentPart content = response.ContentUpdate[0];

                sb.Append(content.Text);
                string text = sb.ToString();

                if (text.Length > 4096)
                {
                    text = text.Substring(0, 4095);
                    embed.WithFooter($"{DateTime.Now.Year} - Ares | {model.DisplayName} (Limite de caracteres alcançado)");
                }

                embed.WithDescription(text);

                if ((DateTime.UtcNow - lastEditDate) > editCooldownTime)
                {
                    await restBotMessage.ModifyAsync(message => message.Embed = embed.Build());
                    lastEditDate = DateTime.UtcNow;
                }
            }
        }

        info.Historics.Add(new ChatHistoricModel(prompt, sb.ToString()));
        await guild.UpdateChatInfoAsync(user, info);

        return sb.ToString();
    }

    private static async Task<string> HandleOpenAiCompletionResponse(
        Guild guild,
        SocketGuildUser user,
        ulong channel,
        string prompt,
        ChatCompletion? completion)
    {
        if (completion == null)
        {
            LogUtil.Error("OpenAI", "Unable to get response.", "");
            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleOpenAiConversation)})";
        }

        ChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAiConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoricModel historic = AiUtil.ConvertToChatHistoric(prompt, responseOpenAi: completion)[0];
        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        return ProcessOpenAiResponse(guild, completion);
    }

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
    /// <b>Anthropic</b> - Conversation Generation
    /// </summary>

    private static async Task<string> HandleAnthropicConversation(
        Guild guild, 
        SocketGuildUser user, 
        ChatModel model, 
        ulong channel, 
        string prompt, 
        GTokenModel tokenData, 
        List<ChatHistoricModel>? historics, 
        RestUserMessage? 
        botMessage = null) 
    {
        string? token = tokenData.Anthropic;

        if (string.IsNullOrWhiteSpace(token))
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        Anthropic.SDK.Messaging.Message userMessage = new Anthropic.SDK.Messaging.Message(RoleType.User, prompt);
        List<Anthropic.SDK.Messaging.Message> messages = AiUtil.GetChatAnthropicMessages(historics);
        messages.Add(userMessage);

        APIAuthentication auth = new APIAuthentication(token);
        AnthropicClient client = new AnthropicClient(auth);

        MessageParameters parameters = new MessageParameters()
        {
            Messages = messages,
            MaxTokens = 2048, // Adjustment to avoid exceeding the 4096-character limit imposed by Discord, preventing resource waste, as any text sent beyond this limit will be truncated.
            Model = model.Model
        };

        MessageResponse? response = await client.Messages.GetClaudeMessageAsync(parameters);

        if (response == null)
        {
            LogUtil.Error
                (
                    "Anthropic",
                    "Unable to get response.",
                    ""
                );

            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleAnthropicConversation)})";
        }

        ChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAiConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoricModel historic = AiUtil.ConvertToChatHistoric(prompt, responseAnthropic: response)[0];
        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        return response.Message.ToString();
    }

    /// <summary>
    /// <b>DeepSeek</b> - Conversation Generation
    /// </summary>

    private static async Task<string> HandleDeepSeekConversation(
        Guild guild, 
        SocketGuildUser user, 
        ChatModel model, 
        ulong channel, 
        string prompt, 
        GTokenModel tokenData, 
        List<ChatHistoricModel>? historics,
        RestUserMessage? botMessage = null)
    {
        string? token = tokenData.Deepseek;

        if (string.IsNullOrWhiteSpace(token))
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        DeepSeek.Core.Models.Message userMessage = DeepSeek.Core.Models.Message.NewUserMessage(prompt);
        List<DeepSeek.Core.Models.Message> messages = AiUtil.GetChatDeepSeekMessages(historics);
        messages.Add(userMessage);

        DeepSeekClient client = new DeepSeekClient(token);

        var request = new ChatRequest
        {
            Messages = messages,
            Model = model.Model,
            MaxTokens = 2048 // Adjustment to avoid exceeding the 4096-character limit imposed by Discord, preventing resource waste, as any text sent beyond this limit will be truncated.
        };

        ChatResponse? response = await client.ChatAsync(request, new CancellationToken());

        if (response == null)
        {
            LogUtil.Error
                (
                    "DeepSeek",
                    "Unable to get response.", 
                    (client.ErrorMsg != null ? client.ErrorMsg : "Unknown")
                );

            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleDeepSeekConversation)})";
        }

        Choice? choice = response.Choices.FirstOrDefault();

        if (choice == null || choice.Message == null || choice.Message.Content == null)
        {
            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleDeepSeekConversation)})";
        }

        ChatInfoModel? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAiConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoricModel? historic = AiUtil.ConvertToChatHistoric(prompt, responseDeepSeek: response)[0];

        if (historic != null)
        {
            info.Historics.Add(historic);
            await guild.UpdateChatInfoAsync(user, info);
        }

        return choice.Message.Content;
    }

    private static void HandleVerifyParameters(Guild guild, IGuildUser user, ChatModel model, string prompt)
    {
        // Parameter validation
        if (guild == null)
            throw new ArgumentNullException(nameof(guild), "There was an internal issue identifying the guild. Please check if the guild was provided correctly.");

        if (user == null)
            throw new ArgumentNullException(nameof(user), "There was an internal issue identifying the user. Please check if the user was provided correctly.");

        if (model == null)
            throw new ArgumentNullException(nameof(model), "There was an internal issue identifying the model. Please check if the model was provided correctly.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentNullException(nameof(prompt), "There was an internal issue identifying the prompt. Please check if the prompt was provided correctly.");
    }
}