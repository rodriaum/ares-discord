namespace Ares.Core.Database.Model.Token;

public class GTokenModel
{
    public string? OpenAi { get; set; }
    public string? Anthropic { get; set; }
    public string? Deepseek { get; set; }
    public string? Imgur { get; set; }

    public GTokenModel(string openai = "", string anthropic = "", string deepseek = "", string? imgur = "")
    {
        this.OpenAi = openai;
        this.Anthropic = anthropic;
        this.Deepseek = deepseek;
        this.Imgur = imgur;
    }
}