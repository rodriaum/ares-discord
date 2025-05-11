/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects.Image;
using Ares.Core.Util;
using OpenAI.Images;
using System.Text.Json.Serialization;

namespace Ares.Core.Objects.Chat.Image;

public class ImageGenOptions
{
    [JsonInclude]
    [JsonPropertyName("quality")]
    public ImageQuality Quality;

    [JsonInclude]
    [JsonPropertyName("size")]
    public ImageSize Size;

    [JsonInclude]
    [JsonPropertyName("style")]
    public ImageStyle Style;

    public ImageGenOptions(ImageQuality quality = ImageQuality.Standard, ImageSize size = ImageSize.W1024xH1024, ImageStyle style = ImageStyle.Natural)
    {
        Quality = quality;
        Size = size;
        this.Style = style;
    }

    /// <summary>
    /// Obtém as opções de geração de imagem da OpenAI.
    /// </summary>
    /// <param name="options">Opções de geração de imagem customizado.</param>
    public ImageGenerationOptions ToImageGenerationOptions()
    {
        GeneratedImageQuality quality = this.Quality switch
        {
            ImageQuality.Standard => GeneratedImageQuality.Standard,
            _ => GeneratedImageQuality.High,
        };

        GeneratedImageStyle style = this.Style switch
        {
            ImageStyle.Vivid => GeneratedImageStyle.Vivid,
            _ => GeneratedImageStyle.Natural,
        };

        GeneratedImageSize size = this.Size switch
        {
            ImageSize.W1792xH1024 => GeneratedImageSize.W1792xH1024,
            ImageSize.W1024xH1792 => GeneratedImageSize.W1024xH1024,
            ImageSize.W1024xH1024 => GeneratedImageSize.W1024xH1024,
            ImageSize.W512xH512 => GeneratedImageSize.W512xH512,
            _ => GeneratedImageSize.W256xH256
        };

        return new ImageGenerationOptions()
        {
            Quality = quality,
            Style = style,
            Size = size
        };
    }

    /// <summary>
    /// Constructs a Image Gen Options.
    /// </summary>
    /// <param name="optionsOpenAi">Response containing an image options by OpenAI.</param>
    public static List<ImageGenOptions> From(ImageGenerationOptions? optionsOpenAi = null)
    {
        var options = new List<ImageGenOptions>();

        if (optionsOpenAi != null)
        {
            var quality = optionsOpenAi.Quality ?? GeneratedImageQuality.Standard;
            var size = optionsOpenAi.Size ?? GeneratedImageSize.W1024xH1024;
            var style = optionsOpenAi.Style ?? GeneratedImageStyle.Natural;

            try
            {
                var imageQuality = (ImageQuality)Enum.Parse(typeof(ImageQuality), quality.ToString(), ignoreCase: true);
                var imageSize = (ImageSize)Enum.Parse(typeof(ImageSize), size.ToString(), ignoreCase: true);
                var imageStyle = (ImageStyle)Enum.Parse(typeof(ImageStyle), style.ToString(), ignoreCase: true);

                options.Add(new ImageGenOptions(imageQuality, imageSize, imageStyle));
            }
            catch (ArgumentException e)
            {
                AresLogger.Log(nameof(From), "Could not convert a class to ImageGenOptions.", e.Message, severity: Severity.Error);
            }
        }

        return options;
    }
}