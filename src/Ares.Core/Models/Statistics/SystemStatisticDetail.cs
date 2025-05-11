using Ares.Core.Objects;
using Ares.Core.Util;
using System.Text.Json.Serialization;

namespace Ares.Core.Models.Statistics;

public class SystemStatisticDetail
{
    [JsonInclude]
    [JsonPropertyName("category")]
    public SystemStatisticCategory Category;

    /// <summary>
    /// The value of the statistic. If the category is ram, this value is in bytes, or if is cpu, this value is in percentage.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("value")]
    public Object Value;

    [JsonInclude]
    [JsonPropertyName("timestamp")]
    public long Timestamp;

    public SystemStatisticDetail(SystemStatisticCategory category, Object value, long timestamp = 0)
    {
        Category = category;
        Value = value;
        Timestamp = timestamp != 0 ? timestamp : TimeUtil.CurrentTimeMillis();
    }
}