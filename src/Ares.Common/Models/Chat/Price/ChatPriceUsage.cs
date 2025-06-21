/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Objects.Image;
using System.Text.Json.Serialization;

namespace Ares.Common.Models.Chat.Price;

/// <summary>
/// Represents the price in dollars per token used in the request and response.
/// </summary>
/// <remarks>
/// Inspired by the source code available at:
/// <see href="https://github.com/openai/openai-dotnet/blob/main/src/Custom/Chat/ChatTokenUsage.cs" />
/// </remarks>
public class ChatPriceUsage
{
    /// <summary> 
    /// Price per 1M token at completion generated.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("outputPriceToken")]
    public decimal OutputPriceToken { get; }

    /// <summary> 
    /// Price per 1M token at request process.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("inputPriceToken")]
    public decimal InputPriceToken { get; }

    /// <summary> 
    /// Price per image.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("inputPricePerImage")]
    public decimal InputPricePerImage { get; set; }

    /// <summary> 
    /// Price in detail. (Optional)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("chatPriceUsageDetail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatPriceUsageDetail>? ChatPriceUsageDetail { get; set; }

    public ChatPriceUsage(decimal outputPriceToken = 0, decimal inputPriceToken = 0, decimal inputPricePerImage = 0, List<ChatPriceUsageDetail>? chatPriceUsageDetail = null)
    {
        OutputPriceToken = outputPriceToken;
        InputPriceToken = inputPriceToken;
        InputPricePerImage = inputPricePerImage;
        ChatPriceUsageDetail = chatPriceUsageDetail ?? new List<ChatPriceUsageDetail>();
    }

    public decimal OutputPriceTokenPerToken()
    {
        return OutputPriceToken / 1_000_000m;
    }

    public decimal InputPriceTokenPerToken()
    {
        return OutputPriceToken / 1_000_000m;
    }

    public decimal TotalTextChatPricePerToken()
    {
        return OutputPriceTokenPerToken() + InputPriceTokenPerToken();
    }

    public decimal TotalTextChatPrice()
    {
        return OutputPriceToken + InputPriceToken;
    }

    public decimal TotalChatPrice()
    {
        return TotalTextChatPrice() + InputPricePerImage;
    }

    public decimal TotalImageChatPrice(ImageQuality quality, ImageSize size)
    {
        return ChatPriceUsageDetail?.Find(x => x.Quality == quality && x.Size == size)?.Price ?? 0;
    }
}