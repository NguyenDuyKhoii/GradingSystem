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
        decimal AverageScore,       // Mean (μ)
        decimal MedianScore,        // Median
        decimal StandardDeviation,  // σ (Độ lệch chuẩn)
        decimal Variance,           // σ² (Phương sai)
        decimal Skewness,           // Hệ số bất đối xứng phân phối điểm
        decimal MinScore,
        decimal MaxScore,
        decimal PassRate,
        decimal FailRate,
        decimal ExcellentRate,
        List<LetterGradeDistributionDto> LetterGradeDistribution, // Phân phối điểm chuẩn (Bell Curve)
        List<ScoreBucketDto> ScoreDistribution,                   // 10 khoảng điểm [0,1) .. [9,10]
        List<CriteriaDifficultyAnalysisDto> CriteriaAnalysis,      // Phân tích chi tiết độ khó từng tiêu chí
        ExamDifficultyAssessmentDto DifficultyAssessment          // Đánh giá tổng quan & khuyến nghị cho bộ môn
    );

    public record LetterGradeDistributionDto(
        string Grade,           // "A", "B", "C", "D", "F"
        string ScoreRange,      // "8.5 - 10.0", "7.0 - 8.4", ...
        int Count,
        decimal Percentage,
        string Description);

    public record ScoreBucketDto(string Label, int Count);

    public record CriteriaDifficultyAnalysisDto(
        int CriteriaId,
        string CriteriaName,
        decimal MaxPoints,
        decimal Weight,
        decimal AverageScore,
        decimal AverageScorePercent,
        int ZeroScoreCount,           // Số SV bị 0 điểm hoàn toàn ở tiêu chí này
        decimal ZeroScoreRate,        // % SV bị 0 điểm
        int StudentsMissingPoints,    // Số SV bị mất điểm (< 100% max)
        decimal MissRate,             // % SV bị mất điểm
        int DifficultyRank,           // Thứ tự độ khó (1 = khó nhất)
        string AssessmentNote         // Gợi ý cải tiến đề thi
    );

    public record ExamDifficultyAssessmentDto(
        string OverallDifficulty,     // "Rất khó", "Khó", "Vừa sức / Chuẩn phân hóa", "Dễ"
        string DifferentiationLevel,   // "Độ phân hóa cao", "Độ phân hóa trung bình", "Độ phân hóa thấp"
        string RecommendationNotes,
        string HardestCriterionName,
        string EasiestCriterionName);

    public class GetExamClassAnalyticsQueryHandler : IRequestHandler<GetExamClassAnalyticsQuery, ExamClassAnalyticsDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetExamClassAnalyticsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExamClassAnalyticsDto?> Handle(GetExamClassAnalyticsQuery request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return null;

            var submissions = await _context.Submissions
                .Where(s => s.ExamClassId == request.ExamClassId)
                .Include(s => s.Grade)
                    .ThenInclude(g => g!.GradeDetails)
                        .ThenInclude(gd => gd.RubricCriteria)
                .ToListAsync(cancellationToken);

            int totalSubmissions = submissions.Count;

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
                    0, 0, 0, 0, 0, 0, 0, 0, 100, 0,
                    GetEmptyGradeDistributions(),
                    GetEmptyBuckets(),
                    new List<CriteriaDifficultyAnalysisDto>(),
                    new ExamDifficultyAssessmentDto(
                        "Chưa có dữ liệu",
                        "Chưa có dữ liệu",
                        "Lớp thi chưa có bài nào hoàn tất chấm điểm.",
                        "N/A",
                        "N/A"
                    )
                );
            }

            var scores = gradedSubmissions.Select(s => s.Grade!.TotalScore).ToList();

            // 1. Academic Statistical Metrics: Mean, Median, Min, Max
            decimal avgScore = scores.Average();
            decimal minScore = scores.Min();
            decimal maxScore = scores.Max();

            var sortedScores = scores.OrderBy(x => x).ToList();
            decimal medianScore = (totalGraded % 2 == 1)
                ? sortedScores[totalGraded / 2]
                : (sortedScores[(totalGraded / 2) - 1] + sortedScores[totalGraded / 2]) / 2.0m;

            // 2. Variance, Standard Deviation & Skewness
            double meanDouble = (double)avgScore;
            double sumSquaredDiff = scores.Sum(s => Math.Pow((double)s - meanDouble, 2));
            double varianceDouble = totalGraded > 1 ? sumSquaredDiff / (totalGraded - 1) : 0;
            double stdDevDouble = Math.Sqrt(varianceDouble);

            double skewnessDouble = 0;
            if (totalGraded > 2 && stdDevDouble > 0.0001)
            {
                double sumCubedDiff = scores.Sum(s => Math.Pow((double)s - meanDouble, 3));
                skewnessDouble = (sumCubedDiff / totalGraded) / Math.Pow(stdDevDouble, 3);
            }

            // 3. Pass, Fail, Excellent Rates
            decimal passRate = (decimal)scores.Count(s => s >= 5.0m) / totalGraded * 100m;
            decimal failRate = 100m - passRate;
            decimal excellentRate = (decimal)scores.Count(s => s >= 8.5m) / totalGraded * 100m;

            // 4. Letter Grade Distribution (A, B, C, D, F) - Bell Curve
            var countA = scores.Count(s => s >= 8.5m);
            var countB = scores.Count(s => s >= 7.0m && s < 8.5m);
            var countC = scores.Count(s => s >= 5.5m && s < 7.0m);
            var countD = scores.Count(s => s >= 5.0m && s < 5.5m);
            var countF = scores.Count(s => s < 5.0m);

            var letterGradeDist = new List<LetterGradeDistributionDto>
            {
                new("A", "8.5 - 10.0", countA, Math.Round((decimal)countA / totalGraded * 100m, 1), "Xuất sắc / Giỏi"),
                new("B", "7.0 - 8.4", countB, Math.Round((decimal)countB / totalGraded * 100m, 1), "Khá"),
                new("C", "5.5 - 6.9", countC, Math.Round((decimal)countC / totalGraded * 100m, 1), "Trung bình khá"),
                new("D", "5.0 - 5.4", countD, Math.Round((decimal)countD / totalGraded * 100m, 1), "Trung bình (Đạt chuẩn tối thiểu)"),
                new("F", "< 5.0", countF, Math.Round((decimal)countF / totalGraded * 100m, 1), "Không đạt (Trượt môn)")
            };

            // 5. Score Distribution - 10 buckets [0,1), [1,2), ..., [9,10]
            var buckets = GetEmptyBuckets();
            foreach (var score in scores)
            {
                int bucketIndex = Math.Min((int)Math.Floor((double)score), 9);
                buckets[bucketIndex] = new ScoreBucketDto(buckets[bucketIndex].Label, buckets[bucketIndex].Count + 1);
            }

            // 6. Detailed Criteria Difficulty & Failure Analysis
            var criteriaDetails = gradedSubmissions
                .SelectMany(s => s.Grade!.GradeDetails)
                .Where(gd => gd.RubricCriteria != null)
                .GroupBy(gd => new
                {
                    gd.RubricCriteriaId,
                    gd.RubricCriteria!.CriteriaName,
                    gd.RubricCriteria.MaxPoints,
                    gd.RubricCriteria.Weight
                })
                .Select(g =>
                {
                    decimal maxPts = g.Key.MaxPoints;
                    decimal avgCritScore = g.Average(gd => gd.Score);
                    decimal avgPercent = maxPts > 0 ? (avgCritScore / maxPts) * 100m : 0;
                    int zeroCount = g.Count(gd => gd.Score == 0);
                    decimal zeroRate = totalGraded > 0 ? (decimal)zeroCount / totalGraded * 100m : 0;
                    int missingCount = g.Count(gd => gd.Score < maxPts);
                    decimal missRate = totalGraded > 0 ? (decimal)missingCount / totalGraded * 100m : 0;

                    return new
                    {
                        g.Key.RubricCriteriaId,
                        g.Key.CriteriaName,
                        MaxPoints = maxPts,
                        g.Key.Weight,
                        AverageScore = avgCritScore,
                        AverageScorePercent = avgPercent,
                        ZeroScoreCount = zeroCount,
                        ZeroScoreRate = zeroRate,
                        StudentsMissingPoints = missingCount,
                        MissRate = missRate
                    };
                })
                .OrderBy(c => c.AverageScorePercent) // hardest first
                .ToList();

            int rank = 1;
            var criteriaAnalysis = criteriaDetails.Select(c =>
            {
                string assessmentNote = c.MissRate switch
                {
                    >= 70m => $"Tiêu chí có tỷ lệ mất điểm rất cao ({c.MissRate:F1}%). Đề thi có thể quá khó, thiếu thời gian hoặc yêu cầu chưa rõ ràng.",
                    >= 40m => $"Mức độ phân hóa tốt. {c.MissRate:F1}% sinh viên bị trừ điểm một phần.",
                    _ => $"Tiêu chí dễ ({100m - c.MissRate:F1}% sinh viên đạt điểm tuyệt đối). Đề thi nên xem xét nâng cao độ phức tạp."
                };

                return new CriteriaDifficultyAnalysisDto(
                    c.RubricCriteriaId,
                    c.CriteriaName,
                    c.MaxPoints,
                    c.Weight,
                    Math.Round(c.AverageScore, 2),
                    Math.Round(c.AverageScorePercent, 1),
                    c.ZeroScoreCount,
                    Math.Round(c.ZeroScoreRate, 1),
                    c.StudentsMissingPoints,
                    Math.Round(c.MissRate, 1),
                    rank++,
                    assessmentNote
                );
            }).ToList();

            // 7. Overall Exam Difficulty & Differentiation Assessment
            string overallDifficulty = avgScore switch
            {
                < 4.5m => "Rất khó (Tỷ lệ không đạt cao, sinh viên gặp nhiều trở ngại)",
                < 6.0m => "Khó (Đề thi có tính thách thức cao)",
                <= 7.5m => "Vừa sức (Đạt chuẩn phân hóa kiến thức học phần)",
                _ => "Tương đối dễ (Đa phần sinh viên đạt điểm khá giỏi)"
            };

            string differentiationLevel = stdDevDouble switch
            {
                >= 2.0 => "Độ phân hóa Cao (Phân tách rõ rệt trình độ giữa các nhóm sinh viên)",
                >= 1.2 => "Độ phân hóa Trung bình (Mức độ phân hóa chuẩn mực)",
                _ => "Độ phân hóa Thấp (Điểm số có xu hướng tập trung cục bộ)"
            };

            var hardestCriterion = criteriaAnalysis.FirstOrDefault()?.CriteriaName ?? "N/A";
            var easiestCriterion = criteriaAnalysis.LastOrDefault()?.CriteriaName ?? "N/A";

            string recommendation = $"Điểm trung vị đạt {medianScore:F2}/10. Độ lệch chuẩn σ = {stdDevDouble:F2}. " +
                (skewnessDouble > 0.5 ? "Phổ điểm lệch phải (đa số sinh viên dưới mức trung bình), đề xuất giảng viên củng cố phần kiến thức thuộc tiêu chí khó nhất. " :
                 skewnessDouble < -0.5 ? "Phổ điểm lệch trái (đa số sinh viên đạt điểm cao), đề thi có thể tăng thêm câu hỏi thử thách. " :
                 "Phổ điểm phân bố đều theo hình chuông Gauss chuẩn, đề thi đạt chất lượng khảo thí tốt. ") +
                $"Tiêu chí thách thức nhất: '{hardestCriterion}'.";

            var assessmentDto = new ExamDifficultyAssessmentDto(
                overallDifficulty,
                differentiationLevel,
                recommendation,
                hardestCriterion,
                easiestCriterion
            );

            return new ExamClassAnalyticsDto(
                examClass.Id,
                examClass.Class?.ClassCode ?? "Unknown",
                examClass.Subject?.SubjectCode ?? "Unknown",
                examClass.Subject?.SubjectName ?? "Unknown",
                examClass.Semester,
                totalSubmissions,
                totalGraded,
                Math.Round(avgScore, 2),
                Math.Round(medianScore, 2),
                Math.Round((decimal)stdDevDouble, 2),
                Math.Round((decimal)varianceDouble, 2),
                Math.Round((decimal)skewnessDouble, 2),
                Math.Round(minScore, 2),
                Math.Round(maxScore, 2),
                Math.Round(passRate, 1),
                Math.Round(failRate, 1),
                Math.Round(excellentRate, 1),
                letterGradeDist,
                buckets,
                criteriaAnalysis,
                assessmentDto
            );
        }

        private static List<ScoreBucketDto> GetEmptyBuckets()
        {
            return Enumerable.Range(0, 10)
                .Select(i => new ScoreBucketDto($"{i}-{i + 1}", 0))
                .ToList();
        }

        private static List<LetterGradeDistributionDto> GetEmptyGradeDistributions()
        {
            return new List<LetterGradeDistributionDto>
            {
                new("A", "8.5 - 10.0", 0, 0, "Xuất sắc / Giỏi"),
                new("B", "7.0 - 8.4", 0, 0, "Khá"),
                new("C", "5.5 - 6.9", 0, 0, "Trung bình khá"),
                new("D", "5.0 - 5.4", 0, 0, "Trung bình"),
                new("F", "< 5.0", 0, 0, "Không đạt")
            };
        }
    }
}
