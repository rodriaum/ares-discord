using Anthropic.SDK.Messaging;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Chat;
using DeepSeek.Core.Models;
using OpenAI.Chat;

namespace Ares.src.Utils.Extra;

public class AiUtil
{
    /// <summary>
    /// <b>Anthropic</b> - Constrói um histórico de chat para Anthropic a partir de uma resposta de mensagem.
    /// </summary>
    /// <param name="prompt">Texto de entrada enviado pelo usuário.</param>
    /// <param name="channel">Identificador do canal onde ocorreu a interação.</param>
    /// <param name="response">Resposta gerada pela IA da Anthropic.</param>
    public static ChatHistoric ConvertMessageResponseToChatHistoric(string prompt, ulong channel, MessageResponse response)
    {
        return new ChatHistoric
        (
            channel: channel,
            model: response.Model,
            prompt: prompt,
            response: response.Message.ToString(),
            usage: new ChatValueUsage(response.Usage.OutputTokens, response.Usage.InputTokens)
        );
    }

    /// <summary>
    /// <b>Anthropic</b> - Obtém mensagens do histórico de chat da Anthropic.
    /// </summary>
    /// <param name="historics">Lista de históricos de chat armazenados.</param>
    public static List<Anthropic.SDK.Messaging.Message> GetChatAnthropicMessages(List<ChatHistoric>? historics)
    {
        if (historics == null || historics.Count == 0)
        {
            return new List<Anthropic.SDK.Messaging.Message>();
        }

        List<Anthropic.SDK.Messaging.Message> messages = new List<Anthropic.SDK.Messaging.Message>();

        foreach (ChatHistoric historic in historics)
        {
            if (!historic.Active) continue;

            if (!string.IsNullOrWhiteSpace(historic.Prompt))
            {
                messages.Add(new Anthropic.SDK.Messaging.Message(RoleType.User, historic.Prompt));
            }

            if (!string.IsNullOrWhiteSpace(historic.Response))
            {
                messages.Add(new Anthropic.SDK.Messaging.Message(RoleType.Assistant, historic.Response));
            }
        }

        return messages;
    }

    /// <summary>
    /// <b>Anthropic</b> - Converte um <see cref="RoleType"/> da Anthropic para um <see cref="ChatRole"/>.
    /// </summary>
    public static ChatRole ConvertAnthropicRole(RoleType role)
    {
        return role switch
        {
            RoleType.User => ChatRole.User,
            RoleType.Assistant => ChatRole.Assistant,
            _ => ChatRole.None
        };
    }

    /// <summary>
    /// <b>OpenAI</b> - Constrói um histórico de chat para OpenAI a partir de um ChatCompletion.
    /// </summary>
    /// <param name="prompt">Texto de entrada enviado pelo usuário.</param>
    /// <param name="channel">Identificador do canal onde ocorreu a interação.</param>
    /// <param name="completion">Resposta gerada pela IA da OpenAI.</param>
    public static ChatHistoric ConvertChatCompletionToChatHistoric(string prompt, ulong channel, ChatCompletion completion)
    {
        var content = completion.Content[0];
        string? imageUrl = content.ImageUri?.AbsoluteUri;

        return new ChatHistoric
        (
            channel: channel,
            model: completion.Model,
            prompt: prompt,
            response: content.Text,
            imageUrl: imageUrl,
            usage: new ChatValueUsage(completion.Usage.OutputTokenCount, completion.Usage.InputTokenCount),
            timestamp: completion.CreatedAt.Ticks
        );
    }

    /// <summary>
    /// <b>OpenAI</b> - Cria uma mensagem de usuário a partir do histórico.
    /// </summary>
    public static ChatMessage CreateUserMessage(ChatHistoric historic)
    {
        return ChatMessage.CreateUserMessage(historic.Prompt);
    }

    /// <summary>
    /// <b>OpenAI</b> - Cria uma mensagem de sistema a partir do histórico.
    /// </summary>
    public static ChatMessage? CreateSystemMessage(ChatHistoric historic)
    {
        return ChatMessage.CreateSystemMessage(historic.Response);
    }

    /// <summary>
    /// <b>OpenAI</b> - Obtém mensagens do histórico de chat da OpenAI.
    /// </summary>
    /// <param name="historics">Lista de históricos de chat armazenados.</param>
    public static List<ChatMessage> GetChatOpenAiMessages(List<ChatHistoric>? historics)
    {
        if (historics == null || historics.Count == 0)
        {
            return new List<ChatMessage>();
        }

        List<ChatMessage> messages = new List<ChatMessage>();

        foreach (ChatHistoric historic in historics)
        {
            if (!historic.Active) continue;

            if (!string.IsNullOrWhiteSpace(historic.Prompt))
            {
                messages.Add(new UserChatMessage(historic.Prompt));
            }

            if (!string.IsNullOrWhiteSpace(historic.Response))
            {
                messages.Add(new AssistantChatMessage(historic.Response));
            }
        }

        return messages;
    }

    /// <summary>
    /// <b>OpenAI</b> - Converte um <see cref="ChatMessageRole"/> da OpenAI para um <see cref="ChatRole"/>.
    /// </summary>
    public static ChatRole ConvertOpenAiRole(ChatMessageRole role)
    {
        return role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            _ => ChatRole.None
        };
    }

    /// <summary>
    /// <b>DeepSeek</b> - Constrói um histórico de chat para DeepSeek a partir de uma resposta de mensagem.
    /// </summary>
    /// <param name="prompt">Texto de entrada enviado pelo usuário.</param>
    /// <param name="channel">Identificador do canal onde ocorreu a interação.</param>
    /// <param name="response">Resposta gerada pela IA da DeepSeek.</param>
    public static ChatHistoric ConvertChatResponseToChatHistoric(string prompt, ulong channel, ChatResponse response)
    {
        Choice? choice = response.Choices.FirstOrDefault();

        if (choice == null || choice.Message == null || choice.Message.Content == null)
        {
            throw new InvalidOperationException($"A escolha ou o conteúdo da mensagem está ausente.\nChannel: {channel}\nPrompt: {prompt}");
        }

        DeepSeek.Core.Models.Usage? usage = response.Usage;

        return new ChatHistoric
        (
            channel: channel,
            model: response.Model,
            prompt: prompt,
            response: choice.Message.Content,
            usage: new ChatValueUsage((usage != null ? usage.CompletionTokens : 0), (usage != null ? usage.PromptTokens : 0))
        );
    }

    /// <summary>
    /// <b>DeepSeek</b> - Obtém mensagens do histórico de chat da DeepSeek.
    /// </summary>
    /// <param name="historics">Lista de históricos de chat armazenados.</param>
    public static List<DeepSeek.Core.Models.Message> GetChatDeepSeekMessages(List<ChatHistoric>? historics)
    {
        if (historics == null || historics.Count == 0)
        {
            return new List<DeepSeek.Core.Models.Message>();
        }
        
        List<DeepSeek.Core.Models.Message> messages = new List<DeepSeek.Core.Models.Message>();

        foreach (ChatHistoric historic in historics)
        {
            if (!historic.Active) continue;

            if (!string.IsNullOrWhiteSpace(historic.Prompt))
            {
                messages.Add(DeepSeek.Core.Models.Message.NewUserMessage(historic.Prompt));
            }

            if (!string.IsNullOrWhiteSpace(historic.Response))
            {
                messages.Add(DeepSeek.Core.Models.Message.NewAssistantMessage(historic.Response));
            }
        }

        return messages;
    }

    /// <summary>
    /// <b>DeepSeek</b> - Converte um <see cref="?"/> da DeepSeek para um <see cref="ChatRole"/>.
    /// </summary>
    public static ChatRole ConvertDeepSeekRole()
    {
        return ChatRole.None;
    }
}