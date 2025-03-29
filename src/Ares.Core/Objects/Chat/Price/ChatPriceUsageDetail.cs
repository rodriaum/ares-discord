/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Chat.Image;

namespace Ares.Core.Objects.Chat.Price;

/// <summary>
/// Representa o preço por algo exato (ex: imagem) usado no pedido.
/// </summary>
public class ChatPriceUsageDetail
{
    public readonly ImageQuality Quality;
    public readonly ImageSize Size;
    public readonly decimal Price;

    public ChatPriceUsageDetail(ImageQuality quality, ImageSize size, decimal price)
    {
        Quality = quality;
        Size = size;
        Price = price;
    }
}