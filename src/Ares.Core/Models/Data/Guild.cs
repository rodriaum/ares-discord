using Ares.Core.Models.Preference;
using Ares.Core.Models.Token;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Data;

/// <summary>
/// Represents a Discord guild.
/// ---
/// Note:
/// This class is structured with nested category-specific classes. 
/// Although some of these classes may currently contain only a single property, 
/// they are kept separate to allow easier adaptation to future changes. 
/// This design also enables saving data by category rather than persisting the entire guild object or individual variables separately. 
/// As a result, it improves performance and optimizes data handling by isolating updates to only the relevant category classes.
/// </summary>
public class Guild
{
    /// <summary>
    /// The unique identifier of the guild.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonInclude]
    [JsonPropertyName("id")]
    public readonly ulong Id;

    /// <summary>
    /// Token data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("token")]
    public GToken Token;

    /// <summary>
    /// Config data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("preference")]
    public GPreference Preferences;

    /// <summary>
    /// Initializes a new instance of the Guild class.
    /// </summary>
    /// <param name="id">The identifier of the guild.</param>
    public Guild(ulong id)
    {
        this.Id = id;

        this.Token = new GToken();
        this.Preferences = new GPreference();
    }
}