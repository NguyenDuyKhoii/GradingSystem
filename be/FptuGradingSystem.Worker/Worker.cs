using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.IO.Compression;

namespace FptuGradingSystem.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private const string StreamKey = "zip-processing-stream";

    public Worker(ILogger<Worker> logger, IConnectionMultiplexer redis, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _redis = redis;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Worker started. Listening to Redis Stream: {StreamKey}", StreamKey);
        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Đọc tin nhắn đầu tiên từ đầu Stream ("0-0")
                var entries = await db.StreamReadAsync(StreamKey, "0-0", count: 1);
                
                if (entries.Length == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var entry = entries[0];
                
                // Trích xuất các tham số từ tin nhắn
                var examClassIdStr = entry.Values.FirstOrDefault(v => v.Name == "examClassId").Value.ToString();
                var zipFilePath = entry.Values.FirstOrDefault(v => v.Name == "zipFilePath").Value.ToString();
                var extractFolderPath = entry.Values.FirstOrDefault(v => v.Name == "extractFolderPath").Value.ToString();

                if (int.TryParse(examClassIdStr, out int examClassId))
                {
                    _logger.LogInformation("Processing ZIP file for ExamClass {ExamClassId} from {ZipFilePath}", examClassId, zipFilePath);
                    await ProcessZipAsync(examClassId, zipFilePath, extractFolderPath, stoppingToken);
                }

                // Xóa tin nhắn đã xử lý để Stream luôn sạch (cơ chế hàng đợi FIFO)
                await db.StreamDeleteAsync(StreamKey, new[] { entry.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing message from Redis Stream.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessZipAsync(int examClassId, string zipFilePath, string extractFolderPath, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var examClass = await context.ExamClasses
            .FirstOrDefaultAsync(x => x.Id == examClassId, cancellationToken);

        if (examClass == null)
        {
            _logger.LogWarning("ExamClass {ExamClassId} not found in database.", examClassId);
            return;
        }

        if (!File.Exists(zipFilePath))
        {
            _logger.LogError("ZIP file {ZipFilePath} does not exist.", zipFilePath);
            examClass.Status = "Lỗi giải nén";
            await context.SaveChangesAsync(cancellationToken);
            await PublishStatusUpdateAsync(examClassId, "Lỗi giải nén", 0);
            return;
        }

        try
        {
            if (Directory.Exists(extractFolderPath))
            {
                Directory.Delete(extractFolderPath, true);
            }

            Directory.CreateDirectory(extractFolderPath);

            _logger.LogInformation("Extracting {ZipFilePath} to {ExtractFolderPath}", zipFilePath, extractFolderPath);
            ZipFile.ExtractToDirectory(zipFilePath, extractFolderPath, overwriteFiles: true);

            var extractedFiles = Directory
                .GetFiles(extractFolderPath, "*", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).StartsWith("."))
                .ToList();

            _logger.LogInformation("Extracted {Count} files. Saving submissions to DB...", extractedFiles.Count);

            foreach (var filePath in extractedFiles)
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath).TrimStart('.');

                // Parse MSSV và tên sinh viên bằng ký tự phân tách
                var parts = fileNameWithoutExt.Split(
                    new[] { '_', '-', ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                var studentId = parts.Length > 0 ? parts[0] : fileNameWithoutExt;
                var studentName = parts.Length > 1
                    ? string.Join(" ", parts.Skip(1))
                    : string.Empty;

                var submission = new Submission
                {
                    ExamClassId = examClassId,
                    StudentId = studentId,
                    StudentName = studentName,
                    FilePath = filePath,
                    FileType = extension,
                    Status = "Unassigned"
                };

                context.Submissions.Add(submission);
            }

            // Chuyển trạng thái sang Grading (Đang chấm) sau khi giải nén xong thành công
            examClass.Status = "Grading";
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully processed ZIP and saved {Count} submissions for ExamClass {ExamClassId}", extractedFiles.Count, examClassId);
            await PublishStatusUpdateAsync(examClassId, "Grading", extractedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ZIP file for ExamClass {ExamClassId}", examClassId);
            examClass.Status = "Lỗi giải nén";
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to save error status to DB for ExamClass {ExamClassId}", examClassId);
            }
            await PublishStatusUpdateAsync(examClassId, "Lỗi giải nén", 0);
        }
    }

    private async Task PublishStatusUpdateAsync(int examClassId, string status, int submissionCount)
    {
        try
        {
            var payloadObj = new
            {
                ExamClassId = examClassId,
                Status = status,
                SubmissionCount = submissionCount
            };
            var payload = System.Text.Json.JsonSerializer.Serialize(payloadObj);
            
            _logger.LogInformation("Publishing status update via Redis Pub/Sub: {Payload}", payload);
            await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal("zip-processed-channel"), payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish status update via Redis Pub/Sub for ExamClass {ExamClassId}", examClassId);
        }
    }
}
