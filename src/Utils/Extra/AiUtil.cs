using Anthropic.SDK.Messaging;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Chat;
using OpenAI.Chat;

namespace Ares.src.Utils.Extra;

public class AiUtil
{
    /// <summary>
    /// <b>ANTHROPIC</b> - Constrói um histórico de chat para Anthropic a partir de uma resposta de mensagem.
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
            usage: new ChatValueUsage(response.Usage.OutputTokens, response.Usage.InputTokens),
            role: ConvertAnthropicRole(response.Role)
        );
    }

    /// <summary>
    /// <b>ANTHROPIC</b> - Obtém mensagens do histórico de chat da Anthropic.
    /// </summary>
    /// <param name="historics">Lista de históricos de chat armazenados.</param>
    public static List<Message> GetChatAnthropicMessages(List<ChatHistoric>? historics)
    {
        if (historics == null || historics.Count == 0)
        {
            return new List<Message>();
        }

        List<Message> messages = new List<Message>();

        foreach (ChatHistoric historic in historics)
        {
            if (!historic.Active) continue;

            if (!string.IsNullOrWhiteSpace(historic.Prompt))
            {
                messages.Add(new Message(RoleType.User, historic.Prompt));
            }

            if (!string.IsNullOrWhiteSpace(historic.Response))
            {
                messages.Add(new Message(RoleType.Assistant, historic.Response));
            }
        }

        return messages;
    }

    /// <summary>
    /// <b>ANTHROPIC</b> - Converte um <see cref="RoleType"/> da Anthropic para um <see cref="ChatRole"/>.
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
            role: ConvertOpenAiRole(completion.Role),
            usage: new ChatValueUsage(completion.Usage.OutputTokens, completion.Usage.InputTokens),
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
}