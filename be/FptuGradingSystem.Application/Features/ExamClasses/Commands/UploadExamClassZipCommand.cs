using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record UploadExamClassZipCommand(
        int ExamClassId,
        string ZipFilePath,
        string ExtractFolderPath
    ) : IRequest<UploadExamClassZipResult>;

    public record UploadExamClassZipResult(
        int ExamClassId,
        int ImportedCount,
        string Status,
        string Message
    );

    public class UploadExamClassZipCommandHandler
        : IRequestHandler<UploadExamClassZipCommand, UploadExamClassZipResult>
    {
        private readonly IApplicationDbContext _context;

        public UploadExamClassZipCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UploadExamClassZipResult> Handle(
            UploadExamClassZipCommand request,
            CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .FirstOrDefaultAsync(x => x.Id == request.ExamClassId, cancellationToken);

            if (examClass == null)
            {
                throw new KeyNotFoundException($"Exam class with ID {request.ExamClassId} not found.");
            }

            if (!File.Exists(request.ZipFilePath))
            {
                throw new FileNotFoundException("Uploaded ZIP file not found.");
            }

            if (Directory.Exists(request.ExtractFolderPath))
            {
                Directory.Delete(request.ExtractFolderPath, true);
            }

            Directory.CreateDirectory(request.ExtractFolderPath);

            try
            {
                ZipFile.ExtractToDirectory(
                    request.ZipFilePath,
                    request.ExtractFolderPath,
                    overwriteFiles: true
                );
            }
            catch (InvalidDataException)
            {
                throw new InvalidDataException("Lỗi giải nén. File ZIP không hợp lệ hoặc bị hỏng.");
            }

            var extractedFiles = Directory
                .GetFiles(request.ExtractFolderPath, "*", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).StartsWith("."))
                .ToList();

            foreach (var filePath in extractedFiles)
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath).TrimStart('.');

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
                    ExamClassId = request.ExamClassId,
                    StudentId = studentId,
                    StudentName = studentName,
                    FilePath = filePath,
                    FileType = extension,
                    Status = "Unassigned"
                };

                _context.Submissions.Add(submission);
            }

            examClass.Status = "Grading";

            await _context.SaveChangesAsync(cancellationToken);

            return new UploadExamClassZipResult(
                request.ExamClassId,
                extractedFiles.Count,
                "Đã giải nén thành công",
                $"Imported {extractedFiles.Count} submissions from ZIP."
            );
        }
    }
}