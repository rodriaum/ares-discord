namespace Ares.src.Objects.Chat;

/// <summary>
/// Representa o preço por algo exato (ex: imagem) usado no pedido.
/// </summary>
public class ChatPriceUsageDetail
{
    public decimal ImageStandard_1024_1024 { get; }
    public decimal ImageStandard_1024_1792 { get; }

    public decimal ImageHD_1024_1024 { get; }
    public decimal ImageHD_1024_1792 { get; }

    public decimal Image_1024_1024 { get; }
    public decimal Image_512_512 { get; }
    public decimal Image_256_256 { get; }

    public ChatPriceUsageDetail
        (
        decimal imageStandard_1024_1024 = 0.0m,
        decimal ImageStandard_1024_1792 = 0.0m,
        decimal ImageHD_1024_1024 = 0.0m,
        decimal imageHD_1024_1792 = 0.0m,
        decimal image_1024_1024 = 0.0m,
        decimal image_512_512 = 0.0m,
        decimal image_256_256 = 0.0m
        )
    {
        ImageStandard_1024_1024 = imageStandard_1024_1024;
        this.ImageStandard_1024_1792 = ImageStandard_1024_1792;
        this.ImageHD_1024_1024 = ImageHD_1024_1024;
        ImageHD_1024_1792 = imageHD_1024_1792;
        Image_1024_1024 = image_1024_1024;
        Image_512_512 = image_512_512;
        Image_256_256 = image_256_256;
    }
}