/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;

namespace Ares.Core.Objects.Chat.Price;

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
    public decimal OutputPriceToken { get; }

    /// <summary> 
    /// Price per 1M token at request process.
    /// </summary>
    public decimal InputPriceToken { get; }

    /// <summary> 
    /// Price per image.
    /// </summary>
    public decimal InputPricePerImage { get; set; }

    /// <summary> 
    /// Price in detail. (Optional)
    /// </summary>
    public List<ChatPriceUsageDetail>? ChatPriceUsageDetail { get; set; }

    public ChatPriceUsage(decimal outPriceToken = 0, decimal inPriceToken = 0, decimal inputPricePerImage = 0, List<ChatPriceUsageDetail>? details = null)
    {
        this.OutputPriceToken = outPriceToken;
        this.InputPriceToken = inPriceToken;
        this.InputPricePerImage = inputPricePerImage;
        this.ChatPriceUsageDetail = details ?? new List<ChatPriceUsageDetail>();
    }

    public decimal OutputPriceTokenPerToken()
    {
        return this.OutputPriceToken / 1_000_000m;
    }

    public decimal InputPriceTokenPerToken()
    {
        return this.OutputPriceToken / 1_000_000m;
    }

    public decimal TotalTextChatPricePerToken()
    {
        return this.OutputPriceTokenPerToken() + this.InputPriceTokenPerToken();
    }

    public decimal TotalTextChatPrice()
    {
        return this.OutputPriceToken + this.InputPriceToken;
    }

    public decimal TotalChatPrice()
    {
        return this.TotalTextChatPrice() + this.InputPricePerImage;
    }

    public decimal TotalImageChatPrice(ImageQuality quality, ImageSize size)
    {
        return this.ChatPriceUsageDetail?.Find(x => x.Quality == quality && x.Size == size)?.Price ?? 0;
    }
}