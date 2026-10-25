using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record CheckPlagiarismCommand(int ExamClassId, decimal ThresholdPercentage = 70.0m) : IRequest<PlagiarismReportDto>;

    public record PlagiarismReportDto(
        int Id,
        int ExamClassId,
        DateTime GeneratedAt,
        decimal ThresholdPercentage,
        int TotalSubmissionsAnalyzed,
        int SuspectPairsCount,
        List<PlagiarismMatchDto> Matches);

    public record PlagiarismMatchDto(
        int Id,
        int SubmissionAId,
        string StudentAId,
        string StudentAName,
        int SubmissionBId,
        string StudentBId,
        string StudentBName,
        decimal SimilarityPercentage,
        string Details);

    public class CheckPlagiarismCommandHandler : IRequestHandler<CheckPlagiarismCommand, PlagiarismReportDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPlagiarismDetectorService _detector;

        public CheckPlagiarismCommandHandler(IApplicationDbContext context, IPlagiarismDetectorService detector)
        {
            _context = context;
            _detector = detector;
        }

        public async Task<PlagiarismReportDto> Handle(CheckPlagiarismCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Submissions)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null)
            {
                throw new KeyNotFoundException($"Exam class with ID {request.ExamClassId} not found.");
            }

            var submissions = examClass.Submissions.ToList();
            if (submissions.Count < 2)
            {
                throw new InvalidOperationException("Cần tối thiểu 2 bài nộp trong lớp thi để đối soát đạo mã nguồn.");
            }

            // Read content of each submission from disk
            var submissionContents = new List<SubmissionCodeContent>();
            foreach (var sub in submissions)
            {
                string content = string.Empty;
                if (!string.IsNullOrEmpty(sub.FilePath) && File.Exists(sub.FilePath))
                {
                    try
                    {
                        content = await File.ReadAllTextAsync(sub.FilePath, cancellationToken);
                    }
                    catch
                    {
                        content = string.Empty;
                    }
                }

                submissionContents.Add(new SubmissionCodeContent(
                    sub.Id,
                    sub.StudentId,
                    sub.StudentName,
                    content));
            }

            // Run Winnowing algorithm
            var matches = _detector.AnalyzeSubmissions(submissionContents, request.ThresholdPercentage);

            // Create new report entity
            var report = new PlagiarismReport
            {
                ExamClassId = request.ExamClassId,
                GeneratedAt = DateTime.UtcNow,
                ThresholdPercentage = request.ThresholdPercentage,
                TotalSubmissionsAnalyzed = submissions.Count,
                SuspectPairsCount = matches.Count,
                Matches = matches.Select(m => new PlagiarismMatch
                {
                    SubmissionAId = m.SubmissionAId,
                    StudentAId = m.StudentAId,
                    StudentAName = m.StudentAName,
                    SubmissionBId = m.SubmissionBId,
                    StudentBId = m.StudentBId,
                    StudentBName = m.StudentBName,
                    SimilarityPercentage = m.SimilarityPercentage,
                    Details = m.Details
                }).ToList()
            };

            _context.PlagiarismReports.Add(report);
            await _context.SaveChangesAsync(cancellationToken);

            return new PlagiarismReportDto(
                report.Id,
                report.ExamClassId,
                report.GeneratedAt,
                report.ThresholdPercentage,
                report.TotalSubmissionsAnalyzed,
                report.SuspectPairsCount,
                report.Matches.Select(m => new PlagiarismMatchDto(
                    m.Id,
                    m.SubmissionAId,
                    m.StudentAId,
                    m.StudentAName,
                    m.SubmissionBId,
                    m.StudentBId,
                    m.StudentBName,
                    m.SimilarityPercentage,
                    m.Details
                )).ToList()
            );
        }
    }
}
