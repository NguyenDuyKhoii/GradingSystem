using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FptuGradingSystem.Infrastructure.Messaging
{
    public class RedisMessagePublisher : IMessagePublisher
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisMessagePublisher> _logger;

        public RedisMessagePublisher(IConnectionMultiplexer redis, ILogger<RedisMessagePublisher> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task PublishAsync(string channel, string message)
        {
            try
            {
                var subscriber = _redis.GetSubscriber();
                var receiverCount = await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
                _logger.LogInformation(
                    "Published message to channel '{Channel}'. Receivers: {ReceiverCount}",
                    channel, receiverCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to channel '{Channel}'", channel);
                throw;
            }
        }
    }
}
