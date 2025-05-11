using Ares.Core.Models.Chat;
using Ares.Core.Models.Statistics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Collection;

/// <summary>
/// Represents a statistic of system.
/// </summary>
public class SystemStatistic
{
    /// <summary>
    /// The unique identifier of the statistic.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonInclude]
    [JsonPropertyName("id")]
    public readonly string Id;

    [JsonInclude]
    [JsonPropertyName("details")]
    public List<SystemStatisticDetail> Details { get; set; }

    /// <summary>
    /// Initializes a new instance of the SystemStatistic class.
    /// </summary>
    /// <param name="id">The identifier of the statistic.</param>
    public SystemStatistic(string? id = null)
    {
        this.Id = id ?? Guid.NewGuid().ToString();
        this.Details = new();
    }
}