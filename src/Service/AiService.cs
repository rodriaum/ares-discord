using Discord.WebSocket;
using OpenAI.Chat;
using Discord;
using Ares.src.Guild.Information;
using OpenAI.Images;
using Ares.src.Utils.Extra;
using Ares.src.Guild.Chat.Sub;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Ares.src.Guild.Token;
using DeepSeek.Core;
using DeepSeek.Core.Models;
using Ares.src.Objects.Model;
using Ares.src.Objects.Language;
using Ares.src.Manager;

namespace Ares.src.Service;

public class AiService
{
    private static string GetMessageByErrorKey(LangCategory category, string key)
    {
        LangManager manager = Core.LangManager;

        string translation = manager.GetTranslation(category, key.Replace("-", "_"));
        return !string.IsNullOrEmpty(translation) ? translation : manager.GetTranslation(category, LangKeys.InvalidRequest) + $"({nameof(GetMessageByErrorKey)})";
    }

    /// <summary>
    /// <b>General</b> - Image Generation
    /// </summary>
    /// 

    // Futuro: Fazer retornar o url e um bool de sucesso.
    public async static Task<string> GenerateImageUrlAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, ImageGenerationOptions options, ulong channel, string prompt) {
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Image)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GuildInformation information = guild.Information;
        GuildTokenData? tokenData = information.Token;

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
            GeneratedImage? image = await GenerateImageAsync(model, openAiToken, prompt, options);

            if (image == null)
            {
                return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(GenerateConversationAsync)})";
            }

            // Usa a URL original se não houver um token Imgur.
            string imageUrl = string.IsNullOrWhiteSpace(imgurToken)
                ? image.ImageUri.OriginalString
                : await RequestUtil.UploadMediaFromUrl(imgurToken, image.ImageUri.OriginalString) ?? image.ImageUri.OriginalString;

            ChatInfo? info = guild.ChatInfoByChannel(user, channel);

            if (info == null)
            {
                LogUtil.Error(nameof(GenerateImageUrlAsync), "It looks like the information could not be accessed.");
                return guild.GetTranslation(LangKeys.CouldNotFindInfo) + $"({nameof(GenerateConversationAsync)})";
            }

            ChatHistoric historic = AiUtil.ConvertGeneratedImageToChatHistoric(prompt, image, imageUrl: imageUrl);
            info.Historics.Add(historic);

            await guild.UpdateChatInfoAsync(user, info);

            return imageUrl;
        }
        catch (Exception e)
        {
            string message = e.Message;

            LogUtil.Error("Generation", "Unable to generate an image.", message);
            return GetMessageByErrorKey(guild.LanguageCategory(), message);
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


    // Futuro: Fazer retornar o texto e um bool de sucesso.
    public async static Task<string> GenerateConversationAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt) {
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Chat)
        {
            return guild.GetTranslation(LangKeys.ModelUnavailable);
        }

        GuildInformation information = guild.Information;
        GuildTokenData? tokenData = information.Token;

        if (tokenData == null)
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        ChatHistoric? historic = null;

        try
        {
            List<ChatHistoric>? historics = guild.ChatHistorics(user, channel: channel);

            switch (model.Category)
            {
                case ModelCategory.OpenAI:
                    return await HandleOpenAIConversation(guild, user, model, channel, prompt, tokenData, historics);

                case ModelCategory.Anthropic:
                    return await HandleAnthropicConversation(guild, user, model, channel, prompt, tokenData, historics);

                case ModelCategory.DeepSeek:
                    return await HandleDeepSeekConversation(guild, user, model, channel, prompt, tokenData, historics);

                default:
                    return guild.GetTranslation(LangKeys.ModelNotFound);
            }
        }
        catch (Exception e)
        {
            if (user != null && channel != 0 && historic != null && !await guild.RemoveConversationAsync(user, channel, historic))
            {
                LogUtil.Error("Generation", "Unable to remove user conversation after internal issue");
            }

            string message = e.Message;

            LogUtil.Error("Generation", "Unable to generate an conversation.", message);
            return GetMessageByErrorKey(guild.LanguageCategory(), message);
        }
    }

    /// <summary>
    /// <b>OpenAI</b> - Conversation Generation
    /// </summary>

    private static async Task<string> HandleOpenAIConversation(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt, GuildTokenData tokenData, List<ChatHistoric>? historics) {
        string? token = tokenData.OpenAi;

        if (string.IsNullOrWhiteSpace(token))
        {
            return guild.GetTranslation(LangKeys.CouldNotFindToken);
        }

        UserChatMessage userMessage = new UserChatMessage(prompt)
        {
            ParticipantName = user.GlobalName
        };

        List<ChatMessage> messages = AiUtil.GetChatOpenAiMessages(historics);
        messages.Add(userMessage);

        ChatClient client = new ChatClient(model.Model, token);

        ChatCompletionOptions options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 2048 // Adjustment to avoid exceeding the 4096-character limit imposed by Discord, preventing resource waste, as any text sent beyond this limit will be truncated.
        };

        ChatCompletion completion = await client.CompleteChatAsync(messages, options: options);

        if (completion == null)
        {
            LogUtil.Error
                (
                    "OpenAI",
                    "Unable to get response.",
                    ""
                );

            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleOpenAIConversation)})";
        }

        ChatInfo? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAIConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoric historic = AiUtil.ConvertChatCompletionToChatHistoric(prompt, completion);
        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        return ProcessOpenAiResponse(guild, completion);
    }

    private static string ProcessOpenAiResponse(Guild.Guild guild, ChatCompletion response)
    {
        ChatMessageContentPart? content = response.Content.FirstOrDefault();

        if (response == null || response.Content == null || content == null)
        {
            return guild.GetTranslation(LangKeys.InvalidRequest) + $"({nameof(HandleOpenAIConversation)})";
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

    private static async Task<string> HandleAnthropicConversation(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt, GuildTokenData tokenData, List<ChatHistoric>? historics) {
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
            Model = model.Model,
            Stream = false,
            Temperature = 1.0m
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

        ChatInfo? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAIConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoric historic = AiUtil.ConvertMessageResponseToChatHistoric(prompt, response);
        info.Historics.Add(historic);

        await guild.UpdateChatInfoAsync(user, info);

        return response.Message.ToString();
    }

    /// <summary>
    /// <b>DeepSeek</b> - Conversation Generation
    /// </summary>

    private static async Task<string> HandleDeepSeekConversation(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt, GuildTokenData tokenData, List<ChatHistoric>? historics)
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

        ChatInfo? info = guild.ChatInfoByChannel(user, channel);

        if (info == null)
        {
            LogUtil.Error(nameof(HandleOpenAIConversation), "It looks like the information could not be accessed.");
            return guild.GetTranslation(LangKeys.CouldNotFindInfo);
        }

        ChatHistoric? historic = AiUtil.ConvertChatResponseToChatHistoric(prompt, info.Id, response);

        if (historic != null)
        {
            info.Historics.Add(historic);
            await guild.UpdateChatInfoAsync(user, info);
        }

        return choice.Message.Content;
    }

    private static void HandleVerifyParameters(Guild.Guild guild, IGuildUser user, ChatModel model, String prompt)
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