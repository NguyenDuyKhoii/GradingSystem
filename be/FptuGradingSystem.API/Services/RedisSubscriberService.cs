using FptuGradingSystem.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Services
{
    public class RedisSubscriberService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<RedisSubscriberService> _logger;

        public RedisSubscriberService(
            IConnectionMultiplexer redis,
            IHubContext<NotificationHub> hubContext,
            ILogger<RedisSubscriberService> logger)
        {
            _redis = redis;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RedisSubscriberService started. Subscribing to: zip-processed-channel");

            var subscriber = _redis.GetSubscriber();

            subscriber.Subscribe("zip-processed-channel", async (channel, message) =>
            {
                _logger.LogInformation("Received message on Redis Pub/Sub channel: {Message}", message);

                try
                {
                    var payload = JsonSerializer.Deserialize<ZipProcessedPayload>(message.ToString());

                    if (payload != null)
                    {
                        _logger.LogInformation("Broadcasting ZIP completion via SignalR: ExamClass {ExamClassId}, Status {Status}, Submissions {SubmissionCount}",
                            payload.ExamClassId, payload.Status, payload.SubmissionCount);

                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveExamClassStatus", 
                            payload.ExamClassId, 
                            payload.Status, 
                            payload.SubmissionCount
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing or broadcasting Redis Pub/Sub message");
                }
            });

            // Keep the task alive until cancellation is requested
            var tcs = new TaskCompletionSource();
            stoppingToken.Register(() => tcs.SetResult());
            return tcs.Task;
        }

        private class ZipProcessedPayload
        {
            public int ExamClassId { get; set; }
            public string Status { get; set; } = string.Empty;
            public int SubmissionCount { get; set; }
        }
    }
}
