/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text.Json.Serialization;

namespace Ares.Core.Objects.Chat;

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
    [JsonInclude]
    [JsonPropertyName("outputTokens")]
    public long OutputTokens { get; }

    /// <summary> Número de tokens no prompt. </summary>
    [JsonInclude]
    [JsonPropertyName("inputTokens")]
    public long InputTokens { get; }

    public ChatValueUsage(long outputTokens = 0, long inputTokens = 0)
    {
        OutputTokens = outputTokens;
        InputTokens = inputTokens;
    }

    public long TotalTokens()
    {
        return OutputTokens + InputTokens;
    }
}