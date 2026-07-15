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
        private readonly IZipProcessingQueue _zipProcessingQueue;

        public UploadExamClassZipCommandHandler(IApplicationDbContext context, IZipProcessingQueue zipProcessingQueue)
        {
            _context = context;
            _zipProcessingQueue = zipProcessingQueue;
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

            // Đẩy tác vụ vào Redis Stream để xử lý bất đồng bộ
            await _zipProcessingQueue.EnqueueAsync(
                request.ExamClassId,
                request.ZipFilePath,
                request.ExtractFolderPath,
                cancellationToken
            );

            // Cập nhật trạng thái ExamClass thành Processing (Đang xử lý)
            examClass.Status = "Processing";
            await _context.SaveChangesAsync(cancellationToken);

            return new UploadExamClassZipResult(
                request.ExamClassId,
                0,
                "Đang giải nén...",
                "File ZIP đã được tải lên thành công và đang được giải nén ngầm."
            );
        }
    }
}