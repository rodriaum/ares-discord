using Ares.Common.Models.Chat;
using System.Text.Json.Serialization;

namespace Ares.Common.Models.Data;

/// <summary>
/// Represents a User of Guild.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonInclude]
    [JsonPropertyName("chat")]
    public UserChat Chat { get; set; }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public User()
    {
        this.Id = 0;
        this.Chat = new();
    }

    /// <summary>
    /// Initializes a new instance of the User class.
    /// </summary>
    /// <param name="id">The identifier of the user.</param>
    public User(ulong id)
    {
        this.Id = id;
        this.Chat = new();
    }
}