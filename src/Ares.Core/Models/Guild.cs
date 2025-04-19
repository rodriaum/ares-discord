using Ares.Core.Models.Chat;
using Ares.Core.Models.Config;
using Ares.Core.Models.Token;
using System.Text.Json.Serialization;

namespace Ares.Core.Models;

/// <summary>
/// Represents a Discord guild (server) with its associated data and operations.
/// </summary>
public class Guild
{
    /// <summary>
    /// The unique identifier of the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("id")]
    public readonly string Id;

    /// <summary>
    /// Token data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("token")]
    public GTokenModel Token;

    /// <summary>
    /// Config data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("config")]
    public GuildConfigData Config;

    /// <summary>
    /// Chat data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("chat")]
    public GChatModel Chat;

    /// <summary>
    /// Initializes a new instance of the Guild class.
    /// </summary>
    /// <param name="id">The identifier of the guild.</param>
    public Guild(string id)
    {
        Id = id;

        Token = new GTokenModel();
        Config = new GuildConfigData();
        Chat = new GChatModel();
    }
}