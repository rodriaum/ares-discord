using Ares.Common.Util;
using System.Text.Json.Serialization;

namespace Ares.Common.Models.Chat.Historic;

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
        Id = id ?? StringUtil.GenerateExclusiveCode(length: 25);
        ChannelId = channelId;
        MessageId = messageId;
        Index = index;
        Text = text;
    }

    public string getFormattedIndex()
    {
        return (this.Index + 1).ToString();
    }
}