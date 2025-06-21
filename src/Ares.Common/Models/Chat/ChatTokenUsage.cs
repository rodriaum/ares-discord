/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text.Json.Serialization;

namespace Ares.Common.Models.Chat;

/// <summary>
/// Represents computed token consumption statistics for a chat completion request.
/// </summary>
/// <remarks>
/// Inspired by the source code available at:
/// <see href="https://github.com/openai/openai-dotnet/blob/main/src/Custom/Chat/ChatTokenUsage.cs" />
/// </remarks>
public class ChatTokenUsage
{
    /// <summary> 
    /// Number of tokens in the generated conclusion.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("outputTokens")]
    public long OutputTokens { get; }

    /// <summary> 
    /// Number of tokens in the prompt.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("inputTokens")]
    public long InputTokens { get; }

    /// <summary> 
    /// Total number of tokens used in the request.
    /// </summary>
    /// <remarks>
    /// This value is the sum of <see cref="OutputTokens"/> and <see cref="InputTokens"/>, if this is 0.
    /// </remarks>
    [JsonInclude]
    [JsonPropertyName("totalTokens")]
    public long TotalTokens { get; }

    public ChatTokenUsage(long outputTokens = 0, long inputTokens = 0, long totalTokens = 0)
    {
        OutputTokens = outputTokens;
        InputTokens = inputTokens;

        TotalTokens = totalTokens == 0 ? outputTokens + inputTokens : totalTokens;
    }
}