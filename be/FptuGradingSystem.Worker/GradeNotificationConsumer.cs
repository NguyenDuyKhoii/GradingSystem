using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FptuGradingSystem.Worker;

/// <summary>
/// Redis Pub/Sub consumer that listens for grade submission events
/// and simulates sending email notifications to students.
/// This is the CONSUMER side of the Message Broker pattern.
/// </summary>
public class GradeNotificationConsumer : BackgroundService
{
    private readonly ILogger<GradeNotificationConsumer> _logger;
    private readonly IConnectionMultiplexer _redis;

    public GradeNotificationConsumer(
        ILogger<GradeNotificationConsumer> logger,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GradeNotificationConsumer started. Subscribing to 'grade:submitted' channel...");

        try
        {
            var subscriber = _redis.GetSubscriber();

            await subscriber.SubscribeAsync(
                RedisChannel.Literal("grade:submitted"),
                (channel, message) =>
                {
                    _logger.LogInformation("====================================");
                    _logger.LogInformation("📬 GRADE EVENT RECEIVED");
                    _logger.LogInformation("Channel: {Channel}", (string)channel!);
                    _logger.LogInformation("Payload: {Message}", (string)message!);
                    _logger.LogInformation("📧 Simulated email notification sent to student.");
                    _logger.LogInformation("====================================");
                });

            _logger.LogInformation("Successfully subscribed to 'grade:submitted'. Waiting for events...");

            // Keep the service running until cancellation is requested
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }

            await subscriber.UnsubscribeAsync(RedisChannel.Literal("grade:submitted"));
            _logger.LogInformation("GradeNotificationConsumer stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GradeNotificationConsumer");
        }
    }
}
