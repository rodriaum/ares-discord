using Ares.Core.Models.Chat;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Data;

/// <summary>
/// Represents a User of Guild.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the guild.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonInclude]
    [JsonPropertyName("id")]
    public readonly ulong Id;

    [JsonInclude]
    [JsonPropertyName("chat")]
    public UserChat Chat { get; set; }

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