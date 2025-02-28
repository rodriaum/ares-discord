namespace Ares.src.Objects.Chat.Image;

public class ImageGenOptions
{
    public readonly ImageQuality Quality;
    public readonly ImageSize Size;
    public readonly ImageStyle Style;

    public ImageGenOptions(ImageQuality quality, ImageSize size, ImageStyle style)
    {
        Quality = quality;
        Size = size;
        this.Style = style;
    }
}