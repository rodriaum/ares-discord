using Ares.Core.Models.Preference;
using Ares.Core.Models.Token;
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
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    /// <summary>
    /// Token data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("token")]
    public GToken Token { get; set; }

    /// <summary>
    /// Config data for the guild.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("preference")]
    public GPreference Preferences { get; set; }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public Guild()
    {
        this.Id = 0;
        this.Token = new GToken();
        this.Preferences = new GPreference();
    }

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