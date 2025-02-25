namespace Ares.src.Objects.Chat;

/// <summary>
/// Representa estatísticas computadas de consumo de tokens para uma solicitação de conclusão de chat.
/// </summary>
/// <remarks>
/// Inspirado no código-fonte disponível em: 
/// <see href="https://github.com/openai/openai-dotnet/blob/main/src/Custom/Chat/ChatTokenUsage.cs" />
/// </remarks>
public class ChatValueUsage
{
        /// <summary> Número de tokens na conclusão gerada. </summary>
        public int OutputTokens { get; }

        /// <summary> Número de tokens no prompt. </summary>
        public int InputTokens { get; }

    public ChatValueUsage(int outputTokens = 0, int inputTokens = 0)
    {
        OutputTokens = outputTokens;
        InputTokens = inputTokens;
    }

    public int TotalTokens()
    {
        return OutputTokens + InputTokens;
    }
}