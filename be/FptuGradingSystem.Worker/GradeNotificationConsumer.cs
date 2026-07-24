using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Worker;

public record GradeItemPayload(
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

public record ClassPublishedPayload(
    int ExamClassId,
    string ClassCode,
    string SubjectName,
    int GradesCount,
    List<GradeItemPayload> Items);

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
        _logger.LogInformation("GradeNotificationConsumer started. Subscribing to 'class:published' channel...");

        try
        {
            var subscriber = _redis.GetSubscriber();

            await subscriber.SubscribeAsync(
                RedisChannel.Literal("class:published"),
                async (channel, message) =>
                {
                    _logger.LogInformation("====================================");
                    _logger.LogInformation("📬 BATCH CLASS PUBLISHED EVENT RECEIVED on channel: {Channel}", (string)channel!);
                    
                    try
                    {
                        var payload = JsonSerializer.Deserialize<ClassPublishedPayload>((string)message!, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (payload?.Items != null && payload.Items.Count > 0)
                        {
                            _logger.LogInformation("Processing batch email delivery for {Count} students in Class {ClassCode}...", payload.Items.Count, payload.ClassCode);

                            foreach (var item in payload.Items)
                            {
                                await _emailService.SendGradeResultEmailAsync(
                                    item.StudentEmail,
                                    item.StudentName,
                                    item.StudentId,
                                    item.ClassCode,
                                    item.SubjectName,
                                    item.TotalScore,
                                    item.LetterGrade ?? "P",
                                    item.IsPassed ?? (item.TotalScore >= 5.0m),
                                    item.GeneralFeedback ?? string.Empty
                                );
                            }

                            _logger.LogInformation("✅ Successfully completed batch email dispatch for ExamClass #{ExamClassId}.", payload.ExamClassId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process class:published event for batch email delivery.");
                    }

                    _logger.LogInformation("====================================");
                });

            _logger.LogInformation("Successfully subscribed to 'class:published'. Waiting for class publish events...");

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }

            await subscriber.UnsubscribeAsync(RedisChannel.Literal("class:published"));
            _logger.LogInformation("GradeNotificationConsumer stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GradeNotificationConsumer");
        }
    }
}
