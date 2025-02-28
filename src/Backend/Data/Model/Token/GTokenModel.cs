namespace Ares.src.Backend.Data.Model.Token;

public class GTokenModel
{
    public string? OpenAi { get; set; }
    public string? Anthropic { get; set; }
    public string? Deepseek { get; set; }
    public string? Imgur { get; set; }

    public GTokenModel(string openai = "", string anthropic = "", string deepseek = "", string? imgur = "")
    {
        OpenAi = openai;
        Anthropic = anthropic;
        Deepseek = deepseek;
        Imgur = imgur;
    }
}