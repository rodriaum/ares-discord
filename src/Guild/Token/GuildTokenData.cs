namespace Ares.src.Guild.Token;

public class GuildTokenData
{
    public string? OpenAi { get; set; }
    public string? Anthropic { get; set; }
    public string? Deepseek { get; set; }

    public GuildTokenData(string openai = "", string anthropic = "", string deepseek = "")
    {
        this.OpenAi = openai;
        this.Anthropic = anthropic;
        this.Deepseek = deepseek;
    }
}