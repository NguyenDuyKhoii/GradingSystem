using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FptuGradingSystem.Worker;

/// <summary>
/// Scheduled background job that generates grade summary reports every 5 minutes.
/// Demonstrates the Background Job requirement of the assignment.
/// </summary>
public class GradeReportWorker : BackgroundService
{
    private readonly ILogger<GradeReportWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public GradeReportWorker(
        ILogger<GradeReportWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GradeReportWorker started. Generating reports every {Interval} minutes.", 
            _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateReportAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating grade report");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("GradeReportWorker stopped.");
    }

    private async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var totalSubmissions = await context.Submissions.CountAsync(cancellationToken);
        var gradedCount = await context.Submissions
            .CountAsync(s => s.Status == "Graded", cancellationToken);
        var draftCount = await context.Submissions
            .CountAsync(s => s.Status == "Draft", cancellationToken);
        var ungradedCount = totalSubmissions - gradedCount - draftCount;

        double avgScore = 0;
        if (await context.Grades.AnyAsync(cancellationToken))
        {
            avgScore = await context.Grades
                .AverageAsync(g => (double)g.TotalScore, cancellationToken);
        }

        var totalClasses = await context.ExamClasses.CountAsync(cancellationToken);

        _logger.LogInformation("╔══════════════════════════════════════╗");
        _logger.LogInformation("║     📊 GRADE SUMMARY REPORT         ║");
        _logger.LogInformation("║  Generated: {Time}  ║", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        _logger.LogInformation("╠══════════════════════════════════════╣");
        _logger.LogInformation("║  Total Exam Classes : {Total,-14} ║", totalClasses);
        _logger.LogInformation("║  Total Submissions  : {Total,-14} ║", totalSubmissions);
        _logger.LogInformation("║  Graded             : {Graded,-14} ║", gradedCount);
        _logger.LogInformation("║  Draft              : {Draft,-14} ║", draftCount);
        _logger.LogInformation("║  Ungraded           : {Ungraded,-14} ║", ungradedCount);
        _logger.LogInformation("║  Average Score      : {Avg,-14:F2} ║", avgScore);
        _logger.LogInformation("╚══════════════════════════════════════╝");
    }
}
