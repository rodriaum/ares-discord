using System.Text.Json.Serialization;

namespace Ares.Core.Models.Chat.Sub;

public class UserChatSnippet
{
    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("channelId")]
    public ulong ChannelId { get; set; }

    [JsonInclude]
    [JsonPropertyName("messageId")]
    public ulong MessageId { get; set; }

    [JsonInclude]
    [JsonPropertyName("index")]
    public uint Index { get; set; }

    [JsonInclude]
    [JsonPropertyName("text")]
    public string Text { get; set; }

    public UserChatSnippet(ulong channelId, ulong messageId, uint index, string text, string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        ChannelId = channelId;
        MessageId = messageId;
        Index = index;
        Text = text;
    }
}