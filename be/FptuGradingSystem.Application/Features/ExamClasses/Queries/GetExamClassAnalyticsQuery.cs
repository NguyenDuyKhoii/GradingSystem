using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record GetExamClassAnalyticsQuery(int ExamClassId) : IRequest<ExamClassAnalyticsDto?>;

    public record ExamClassAnalyticsDto(
        int ExamClassId,
        string ClassCode,
        string SubjectCode,
        string SubjectName,
        string Semester,
        int TotalSubmissions,
        int TotalGraded,
        decimal AverageScore,
        decimal MinScore,
        decimal MaxScore,
        decimal PassRate,
        decimal FailRate,
        decimal ExcellentRate,
        List<ScoreBucketDto> ScoreDistribution,
        List<CriteriaAnalysisDto> CriteriaAnalysis
    );

    public record ScoreBucketDto(string Label, int Count);

    public record CriteriaAnalysisDto(
        string CriteriaName,
        decimal MaxPoints,
        decimal Weight,
        decimal AverageScore,
        decimal AverageScorePercent,
        int StudentsMissingPoints, // students who scored < 70% of maxPoints
        decimal MissRate           // % students who missed points
    );

    public class GetExamClassAnalyticsQueryHandler : IRequestHandler<GetExamClassAnalyticsQuery, ExamClassAnalyticsDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetExamClassAnalyticsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExamClassAnalyticsDto?> Handle(GetExamClassAnalyticsQuery request, CancellationToken cancellationToken)
        {
            // Load the exam class with info
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return null;

            // Load all submissions for this exam class
            var submissions = await _context.Submissions
                .Where(s => s.ExamClassId == request.ExamClassId)
                .Include(s => s.Grade)
                    .ThenInclude(g => g!.GradeDetails)
                    .ThenInclude(gd => gd.RubricCriteria)
                .ToListAsync(cancellationToken);

            int totalSubmissions = submissions.Count;

            // Only graded (non-draft) submissions
            var gradedSubmissions = submissions
                .Where(s => s.Grade != null && s.Status == "Graded")
                .ToList();

            int totalGraded = gradedSubmissions.Count;

            if (totalGraded == 0)
            {
                return new ExamClassAnalyticsDto(
                    examClass.Id,
                    examClass.Class?.ClassCode ?? "Unknown",
                    examClass.Subject?.SubjectCode ?? "Unknown",
                    examClass.Subject?.SubjectName ?? "Unknown",
                    examClass.Semester,
                    totalSubmissions,
                    0,
                    0, 0, 0, 0, 100, 0,
                    GetEmptyBuckets(),
                    new List<CriteriaAnalysisDto>()
                );
            }

            var scores = gradedSubmissions.Select(s => s.Grade!.TotalScore).ToList();

            decimal avgScore = scores.Average();
            decimal minScore = scores.Min();
            decimal maxScore = scores.Max();

            // Pass = TotalScore >= 5 (out of 10)
            decimal passRate = (decimal)scores.Count(s => s >= 5) / totalGraded * 100;
            decimal failRate = 100 - passRate;
            decimal excellentRate = (decimal)scores.Count(s => s >= 8) / totalGraded * 100;

            // Score distribution - 10 buckets [0,1), [1,2), ..., [9,10]
            var buckets = GetEmptyBuckets();
            foreach (var score in scores)
            {
                int bucketIndex = Math.Min((int)Math.Floor((double)score), 9);
                buckets[bucketIndex] = new ScoreBucketDto(buckets[bucketIndex].Label, buckets[bucketIndex].Count + 1);
            }

            // Criteria difficulty analysis
            var criteriaGroups = gradedSubmissions
                .SelectMany(s => s.Grade!.GradeDetails)
                .Where(gd => gd.RubricCriteria != null)
                .GroupBy(gd => new { gd.RubricCriteriaId, gd.RubricCriteria!.CriteriaName, gd.RubricCriteria.MaxPoints, gd.RubricCriteria.Weight })
                .Select(g => new CriteriaAnalysisDto(
                    g.Key.CriteriaName,
                    g.Key.MaxPoints,
                    g.Key.Weight,
                    g.Average(gd => gd.Score),
                    g.Key.MaxPoints > 0 ? g.Average(gd => gd.Score) / g.Key.MaxPoints * 100 : 0,
                    g.Count(gd => g.Key.MaxPoints > 0 && gd.Score / g.Key.MaxPoints < 0.7m),
                    totalGraded > 0 ? (decimal)g.Count(gd => g.Key.MaxPoints > 0 && gd.Score / g.Key.MaxPoints < 0.7m) / totalGraded * 100 : 0
                ))
                .OrderBy(c => c.AverageScorePercent) // worst performing first
                .ToList();

            return new ExamClassAnalyticsDto(
                examClass.Id,
                examClass.Class?.ClassCode ?? "Unknown",
                examClass.Subject?.SubjectCode ?? "Unknown",
                examClass.Subject?.SubjectName ?? "Unknown",
                examClass.Semester,
                totalSubmissions,
                totalGraded,
                Math.Round(avgScore, 2),
                Math.Round(minScore, 2),
                Math.Round(maxScore, 2),
                Math.Round(passRate, 1),
                Math.Round(failRate, 1),
                Math.Round(excellentRate, 1),
                buckets,
                criteriaGroups
            );
        }

        private static List<ScoreBucketDto> GetEmptyBuckets()
        {
            return Enumerable.Range(0, 10)
                .Select(i => new ScoreBucketDto($"{i}-{i + 1}", 0))
                .ToList();
        }
    }
}
