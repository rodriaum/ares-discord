using Ares.Core.Util;
using StackExchange.Redis;

namespace Ares.Core.Database.Redis.Channel;

public class RedisPubSub
{
    private readonly ConnectionMultiplexer? _connection;
    private readonly string[] _channels;
    private readonly Action<RedisChannelMessage> _messageHandler;

    public RedisPubSub(ConnectionMultiplexer connection, Action<RedisChannelMessage> messageHandler, params string[] channels)
    {
        _connection = connection;
        _channels = channels;
        _messageHandler = messageHandler;
    }

    public async void RegisterChannels()
    {
        try
        {
            foreach (string channel in _channels)
            {
                ISubscriber subscriber = _connection.GetSubscriber();
                RedisChannel redisChannel = RedisChannel.Literal(channel);

                try
                {
                    await subscriber.SubscribeAsync(redisChannel, (channel, message) =>
                    {
                        _messageHandler(new RedisChannelMessage
                        {
                            Channel = channel,
                            Message = message
                        });
                    });
                }
                catch (Exception ex)
                {
                    AresLogger.Error("DB: Redis", $"Unable to register Redis channel \"{channel}\".", ex.Message);
                }

                try
                {
                    await subscriber.UnsubscribeAsync(redisChannel);
                }
                catch (Exception) { }
            }
        }
        catch (Exception ex)
        {
            AresLogger.Error("DB: Redis", $"Failed to register Redis channels.", ex.Message);
            RegisterChannels();
        }
    }
}