namespace Ares.src.Objects.Chat.Image;

public class ImageGenOptions
{
    public readonly ImageQuality Quality;
    public readonly ImageSize Size;
    public readonly ImageStyle style;

    public ImageGenOptions(ImageQuality quality, ImageSize size, ImageStyle style)
    {
        Quality = quality;
        Size = size;
        this.style = style;
    }
}