using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Grades.Queries
{
    public record GetSubmissionWithGradeQuery(int SubmissionId) : IRequest<SubmissionGradingDto?>;

    public record SubmissionGradingDto(
        int Id,
        int ExamClassId,
        string StudentId,
        string StudentName,
        string FilePath,
        string FileType,
        string Status,
        GradeDto? Grade);

    public record GradeDto(
        int Id,
        decimal TotalScore,
        string GeneralFeedback,
        DateTime GradedAt,
        int GradedById,
        string GradedByName,
        List<GradeDetailDto> Details);

    public record GradeDetailDto(
        int Id,
        int RubricCriteriaId,
        string CriteriaName,
        decimal MaxPoints,
        decimal Weight,
        decimal Score,
        string Feedback);

    public class GetSubmissionWithGradeQueryHandler : IRequestHandler<GetSubmissionWithGradeQuery, SubmissionGradingDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetSubmissionWithGradeQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SubmissionGradingDto?> Handle(GetSubmissionWithGradeQuery request, CancellationToken cancellationToken)
        {
            var submission = await _context.Submissions
                .Include(s => s.Grade)
                .ThenInclude(g => g!.GradeDetails)
                .ThenInclude(gd => gd.RubricCriteria)
                .Include(s => s.Grade)
                .ThenInclude(g => g!.GradedBy)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission == null) return null;

            GradeDto? gradeDto = null;
            if (submission.Grade != null)
            {
                gradeDto = new GradeDto(
                    submission.Grade.Id,
                    submission.Grade.TotalScore,
                    submission.Grade.GeneralFeedback,
                    submission.Grade.GradedAt,
                    submission.Grade.GradedById,
                    submission.Grade.GradedBy?.FullName ?? "Unknown",
                    submission.Grade.GradeDetails.Select(gd => new GradeDetailDto(
                        gd.Id,
                        gd.RubricCriteriaId,
                        gd.RubricCriteria?.CriteriaName ?? "Unknown",
                        gd.RubricCriteria?.MaxPoints ?? 0,
                        gd.RubricCriteria?.Weight ?? 0,
                        gd.Score,
                        gd.Feedback
                    )).ToList()
                );
            }

            return new SubmissionGradingDto(
                submission.Id,
                submission.ExamClassId,
                submission.StudentId,
                submission.StudentName,
                submission.FilePath,
                submission.FileType,
                submission.Status,
                gradeDto
            );
        }
    }
}
