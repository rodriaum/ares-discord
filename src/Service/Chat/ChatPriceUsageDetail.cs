namespace Ares.src.Service.Chat;

/// <summary>
/// Representa o preço por algo exato (ex: imagem) usado no pedido.
/// </summary>
public class ChatPriceUsageDetail
{
    public double ImageStandard_1024_1024 { get; }
    public double ImageStandard_1024_1792 { get; }

    public double ImageHD_1024_1024 { get; }
    public double ImageHD_1024_1792 { get; }

    public double Image_1024_1024 { get; }
    public double Image_512_512 { get; }
    public double Image_256_256 { get; }

    public ChatPriceUsageDetail
        (
        double imageStandard_1024_1024 = 0, 
        double ImageStandard_1024_1792 = 0.0, 
        double ImageHD_1024_1024 = 0.0, 
        double imageHD_1024_1792 = 0.0,
        double image_1024_1024 = 0.0,
        double image_512_512 = 0.0,
        double image_256_256 = 0.0
        )
    {
        this.ImageStandard_1024_1024 = imageStandard_1024_1024;
        this.ImageStandard_1024_1792 = ImageStandard_1024_1792;
        this.ImageHD_1024_1024 = ImageHD_1024_1024;
        this.ImageHD_1024_1792 = imageHD_1024_1792;
        this.Image_1024_1024 = image_1024_1024;
        this.Image_512_512 = image_512_512;
        this.Image_256_256 = image_256_256;
    }
}