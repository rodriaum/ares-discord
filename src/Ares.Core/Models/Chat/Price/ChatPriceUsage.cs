/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

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
    /// Price per token at completion generated.
    /// </summary>
    public decimal OutputPricePerToken { get; }

    /// <summary> 
    /// Token price in prompt.
    /// </summary>
    public decimal InputPricePerToken { get; }

    /// <summary> 
    /// Price per image.
    /// </summary>
    public decimal InputPricePerImage { get; set; }

    /// <summary> 
    /// Price in detail. (Optional) 
    /// </summary>
    public List<ChatPriceUsageDetail>? ChatPriceUsageDetail { get; set; }

    public ChatPriceUsage(decimal outputPricePerToken = 0, decimal inputPricePerToken = 0, decimal inputPricePerImage = 0, List<ChatPriceUsageDetail>? details = null)
    {
        this.OutputPricePerToken = outputPricePerToken;
        this.InputPricePerToken = inputPricePerToken;
        this.InputPricePerImage = inputPricePerImage;
        this.ChatPriceUsageDetail = details ?? new List<ChatPriceUsageDetail>();
    }

    public decimal TotalTokenPrice()
    {
        return this.OutputPricePerToken + this.InputPricePerToken;
    }

    public decimal TotalPrice()
    {
        return this.TotalTokenPrice() + this.InputPricePerImage;
    }
}