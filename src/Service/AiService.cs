using Discord.WebSocket;
using OpenAI.Chat;
using Discord;
using Ares.src.Guild.Information;
using OpenAI.Images;
using Ares.src.Utils.Extra;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Model;
using Ares.src.Guild.Config;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;


namespace Ares.src.Service;

public class AiService
{

    /// <summary>
    /// Image Generation
    /// </summary>
    /// 

    // Futuro: Fazer retornar o url e um bool de sucesso.
    public async static Task<string> GenerateImageUrlAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, ImageGenerationOptions options, string prompt) {
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Image)
        {
            return "Parece que houve um problema na identificação do modelo. Tente novamente!";
        }

        GuildInformation information = guild.Information;
        GuildConfigData? config = information.Config;

        if (config == null)
        {
            return "Não foi possível encontrar as informações sobre os IDs.";
        }

        string? token = config.OpenAiToken;

        if (string.IsNullOrEmpty(token))
        {
            return "Ops! Parece que o servidor atual não tem um token pré-configurado.";
        }

        try
        {
            return await GenerateImageAsync(model, token, prompt, options);
        }
        catch (Exception)
        {
            return Constant.UNABLE_PERFORM_TASK;
        }
    }

    private static async Task<string> GenerateImageAsync(
        ChatModel model,
        string token,
        string prompt,
        ImageGenerationOptions options)
    {
        ImageClient client = new ImageClient(model.Model, token);
        GeneratedImage image = await client.GenerateImageAsync(prompt, options);
        return image.ImageUri.OriginalString;
    }


    /// <summary>
    /// Conversation Generation
    /// </summary>


    // Futuro: Fazer retornar o texto e um bool de sucesso.
    public async static Task<string> GenerateConversationAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt) {
        HandleVerifyParameters(guild, user, model, prompt);

        if (model.Type != ModelType.Chat)
        {
            return "Parece que houve um problema na identificação do modelo. Tente novamente!";
        }

        GuildInformation information = guild.Information;
        GuildConfigData? config = information.Config;

        if (config == null)
        {
            return "Não foi possível encontrar as informações sobre os IDs.";
        }

        ChatHistoric? historic = null;

        try
        {
            List<ChatHistoric>? historics = guild.ChatHistorics(user);

            switch (model.Category)
            {
                case ModelCategory.OpenAI:
                    return await HandleOpenAIConversation(guild, user, model, channel, prompt, config, historics);

                case ModelCategory.Anthropic:
                    return await HandleAnthropicConversation(guild, user, model, channel, prompt, config, historics);

                default:
                    return "Não foi possível identificar o modelo. Tente novamente!";
            }
        }
        catch (Exception e)
        {
            if (historic != null && !await guild.RemoveConversationAsync(user, historic))
            {
                throw new Exception("Não foi possível remover a conversa do usuário após um problema interno.", e);
            }

            return Constant.UNABLE_PERFORM_TASK;
        }
    }

    private static async Task<string> HandleOpenAIConversation(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt,GuildConfigData config, List<ChatHistoric>? historics) {
        string? token = config.OpenAiToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            return "Ops! Parece que o servidor atual não tem um token OpenAI pré-configurado.";
        }

        UserChatMessage userMessage = new UserChatMessage(prompt)
        {
            ParticipantName = user.GlobalName
        };

        List<ChatMessage> messages = AiUtil.GetChatOpenAiMessages(historics);
        messages.Add(userMessage);

        ChatClient client = new ChatClient(model.Model, token);
        ChatCompletion completion = await client.CompleteChatAsync(messages);

        ChatHistoric historic = AiUtil.ConvertChatCompletionToChatHistoric(prompt, channel, completion);
        await guild.SaveHistoricAsync(user, historic);

        return ProcessOpenAiResponse(completion);
    }

    private static string ProcessOpenAiResponse(ChatCompletion openAiResponse)
    {
        return openAiResponse.FinishReason switch
        {
            ChatFinishReason.Stop => openAiResponse.ToString(),
            ChatFinishReason.Length => "Não será possível prosseguir porque o limite de token estabelecido pelo servidor atual foi excedido.",
            ChatFinishReason.ContentFilter => "Não foi possível gerar porque o sistema identificou palavras ofensivas no canal atual.",
            ChatFinishReason.FunctionCall => "Não foi possível gerar porque o sistema está lento. (FunctionCall)",
            _ => $"Não foi possível gerar a resposta. Motivo: {openAiResponse.FinishReason}"
        };
    }

    private static async Task<string> HandleAnthropicConversation(Guild.Guild guild, SocketGuildUser user, ChatModel model, ulong channel, string prompt,GuildConfigData config, List<ChatHistoric>? historics) {
        string? token = config.AnthropicToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            return "Ops! Parece que o servidor atual não tem um token Anthropic pré-configurado.";
        }

        Message userMessage = new Message(RoleType.User, prompt);
        List<Message> messages = AiUtil.GetChatAnthropicMessages(historics);
        messages.Add(userMessage);

        APIAuthentication auth = new APIAuthentication(token);
        AnthropicClient client = new AnthropicClient(auth);

        MessageParameters parameters = new MessageParameters()
        {
            Messages = messages,
            MaxTokens = 1024,
            Model = model.Model,
            Stream = false,
            Temperature = 1.0m
        };

        MessageResponse response = await client.Messages.GetClaudeMessageAsync(parameters);
       
        ChatHistoric historic = AiUtil.ConvertMessageResponseToChatHistoric(prompt, channel, response);
        await guild.SaveHistoricAsync(user, historic);

        return response.Message.ToString();
    }

    private static void HandleVerifyParameters(Guild.Guild guild, IGuildUser user, ChatModel model, String prompt)
    {
        // Validação de parâmetros
        if (guild == null)
            throw new ArgumentNullException(nameof(guild), "Houve um problema interno ao identificar uma guilda. Por favor, verifique se a guilda foi fornecida corretamente.");

        if (user == null)
            throw new ArgumentNullException(nameof(user), "Houve um problema interno ao identificar um usuário. Por favor, verifique se o usuário foi fornecido corretamente.");

        if (model == null)
            throw new ArgumentNullException(nameof(model), "Houve um problema interno ao identificar o modelo. Por favor, verifique se o modelo foi fornecido corretamente.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentNullException(nameof(prompt), "Houve um problema interno ao identificar o prompt. Por favor, verifique se o prompt foi fornecido corretamente.");
    }
}