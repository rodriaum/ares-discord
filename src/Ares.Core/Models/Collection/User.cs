using Ares.Core.Models.Chat.Sub;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Collection;

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
    [JsonPropertyName("userId")]
    public readonly string UserId;

    [JsonInclude]
    [JsonPropertyName("chats")]
    public Dictionary<ulong, List<GChatInfo>> Chats { get; set; }

    /// <summary>
    /// Initializes a new instance of the User class.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    public User(string userId)
    {
        this.UserId = userId;
        this.Chats = new();
    }
}