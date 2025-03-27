using StackExchange.Redis;

namespace Ares.Core.Database.Redis.Channel;

public class RedisChannelMessage
{
    public string? Channel { get; set; }
    public RedisValue Message { get; set; }
}