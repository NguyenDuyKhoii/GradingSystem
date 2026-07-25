using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record PublishExamClassGradesCommand(int ExamClassId) : IRequest<bool>;

    public class PublishExamClassGradesCommandHandler : IRequestHandler<PublishExamClassGradesCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMessagePublisher _messagePublisher;

        public PublishExamClassGradesCommandHandler(
            IApplicationDbContext context,
            IMessagePublisher messagePublisher)
        {
            _context = context;
            _messagePublisher = messagePublisher;
        }

        public async Task<bool> Handle(PublishExamClassGradesCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .Include(ec => ec.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return false;

            // Update status to Completed
            examClass.Status = "Completed";
            await _context.SaveChangesAsync(cancellationToken);

            var gradedSubmissions = examClass.Submissions
                .Where(s => s.Grade != null)
                .ToList();

            if (gradedSubmissions.Any())
            {
                var gradeEvents = gradedSubmissions.Select(s =>
                {
                    var score = s.Grade!.TotalScore;
                    var letterGrade = CalculateLetterGrade(score);
                    var isPassed = score >= 5.0m;

                    return new
                    {
                        SubmissionId = s.Id,
                        StudentId = s.StudentId,
                        StudentName = s.StudentName,
                        StudentEmail = string.IsNullOrWhiteSpace(s.Email)
                            ? GenerateFptuEmail(s.StudentName, s.StudentId)
                            : s.Email,
                        ClassCode = examClass.Class?.ClassCode ?? "EXAM_CLASS",
                        SubjectName = examClass.Subject?.SubjectName ?? "Practical Exam",
                        TotalScore = score,
                        LetterGrade = letterGrade,
                        IsPassed = isPassed,
                        GeneralFeedback = s.Grade.GeneralFeedback,
                        GradedAt = DateTime.UtcNow
                    };
                }).ToList();

                var batchPayload = new
                {
                    ExamClassId = examClass.Id,
                    ClassCode = examClass.Class?.ClassCode ?? "EXAM_CLASS",
                    SubjectName = examClass.Subject?.SubjectName ?? "Practical Exam",
                    GradesCount = gradeEvents.Count,
                    Items = gradeEvents
                };

                await _messagePublisher.PublishAsync(
                    "class:published",
                    JsonSerializer.Serialize(batchPayload));
            }

            return true;
        }

        private static string CalculateLetterGrade(decimal score) => score switch
        {
            >= 9.0m => "A+",
            >= 8.5m => "A",
            >= 8.0m => "B+",
            >= 7.0m => "B",
            >= 6.5m => "C+",
            >= 5.5m => "C",
            >= 5.0m => "D",
            _ => "F"
        };

        private static string GenerateFptuEmail(string studentName, string studentId)
        {
            return FptuGradingSystem.Domain.Helpers.FptuEmailHelper.GenerateFptuEmail(studentName, studentId);
        }
    }
}
