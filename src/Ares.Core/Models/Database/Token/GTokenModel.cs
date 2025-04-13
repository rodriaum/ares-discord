/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text.Json.Serialization;

namespace Ares.Ares.Core.Models.Database.Token;

public class GTokenModel
{
    [JsonInclude]
    [JsonPropertyName("openai")]
    public string? OpenAi { get; set; }

    [JsonInclude]
    [JsonPropertyName("anthropic")]
    public string? Anthropic { get; set; }

    [JsonInclude]
    [JsonPropertyName("deepseek")]
    public string? Deepseek { get; set; }

    [JsonInclude]
    [JsonPropertyName("xai")]
    public string? xAI { get; set; }

    [JsonInclude]
    [JsonPropertyName("google")]
    public string? Google { get; set; }

    [JsonInclude]
    [JsonPropertyName("imgur")]
    public string? Imgur { get; set; }

    public GTokenModel(string openai = "", string anthropic = "", string deepseek = "", string xai = "", string? google = "", string? imgur = "")
    {
        OpenAi = openai;
        Anthropic = anthropic;
        Deepseek = deepseek;
        xAI = xai;
        Google = google;
        Imgur = imgur;
    }
}