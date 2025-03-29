using Ares.Core.Database.Model.Chat.Sub;
using Ares.Core.Objects.Chat;
using OpenAI.Chat;

namespace Ares.Core.Util;

public class AiUtil
{
    /// <summary>
    /// <b>OpenAI</b> - Cria uma mensagem de usuário a partir do histórico.
    /// </summary>
    public static ChatMessage CreateUserMessage(GChatHistoricModel historic)
    {
        return ChatMessage.CreateUserMessage(historic.Prompt);
    }

    /// <summary>
    /// <b>OpenAI</b> - Cria uma mensagem de sistema a partir do histórico.
    /// </summary>
    public static ChatMessage? CreateSystemMessage(GChatHistoricModel historic)
    {
        return ChatMessage.CreateSystemMessage(historic.Response);
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