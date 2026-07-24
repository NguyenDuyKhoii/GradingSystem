using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Worker;

public record GradeSubmittedPayload(
    int SubmissionId,
    string StudentId,
    string StudentName,
    string StudentEmail,
    string ClassCode,
    string SubjectName,
    decimal TotalScore,
    string? LetterGrade,
    bool? IsPassed,
    string? GeneralFeedback);

public class GradeNotificationConsumer : BackgroundService
{
    private readonly ILogger<GradeNotificationConsumer> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IEmailService _emailService;

    public GradeNotificationConsumer(
        ILogger<GradeNotificationConsumer> logger,
        IConnectionMultiplexer redis,
        IEmailService emailService)
    {
        _logger = logger;
        _redis = redis;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GradeNotificationConsumer started. Subscribing to 'grade:submitted' channel...");

        try
        {
            var subscriber = _redis.GetSubscriber();

            await subscriber.SubscribeAsync(
                RedisChannel.Literal("grade:submitted"),
                async (channel, message) =>
                {
                    _logger.LogInformation("====================================");
                    _logger.LogInformation("📬 GRADE EVENT RECEIVED on channel: {Channel}", (string)channel!);
                    _logger.LogInformation("Payload: {Message}", (string)message!);
                    
                    try
                    {
                        var payload = JsonSerializer.Deserialize<GradeSubmittedPayload>((string)message!, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (payload != null)
                        {
                            await _emailService.SendGradeResultEmailAsync(
                                payload.StudentEmail,
                                payload.StudentName,
                                payload.StudentId,
                                payload.ClassCode,
                                payload.SubjectName,
                                payload.TotalScore,
                                payload.LetterGrade ?? "P",
                                payload.IsPassed ?? (payload.TotalScore >= 5.0m),
                                payload.GeneralFeedback ?? string.Empty
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse grade event payload for email delivery.");
                    }

                    _logger.LogInformation("====================================");
                });

            _logger.LogInformation("Successfully subscribed to 'grade:submitted'. Waiting for events...");

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
