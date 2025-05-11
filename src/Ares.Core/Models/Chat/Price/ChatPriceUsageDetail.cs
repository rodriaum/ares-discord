/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Image;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat.Price;

/// <summary>
/// Representa o preço por algo exato (ex: imagem) usado no pedido.
/// </summary>
public class ChatPriceUsageDetail
{
    [JsonInclude]
    [JsonPropertyName("quality")]
    public readonly ImageQuality Quality;

    [JsonInclude]
    [JsonPropertyName("size")]
    public readonly ImageSize Size;

    [JsonInclude]
    [JsonPropertyName("price")]
    public readonly decimal Price;

    public ChatPriceUsageDetail(ImageQuality quality, ImageSize size, decimal price)
    {
        Quality = quality;
        Size = size;
        Price = price;
    }
}