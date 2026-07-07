using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record UpdateExamClassCommand(
        int Id,
        string ClassCode,
        int SubjectId,
        string Semester,
        int? LecturerId,
        string? Status
    ) : IRequest;

    public class UpdateExamClassCommandHandler : IRequestHandler<UpdateExamClassCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateExamClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateExamClassCommand request, CancellationToken cancellationToken)
        {
            var classCode = request.ClassCode.Trim();
            var semester = request.Semester.Trim();

            if (string.IsNullOrWhiteSpace(classCode))
            {
                throw new ArgumentException("Class code is required.");
            }

            if (string.IsNullOrWhiteSpace(semester))
            {
                throw new ArgumentException("Semester is required.");
            }

            var examClass = await _context.ExamClasses
                .FirstOrDefaultAsync(ec => ec.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Exam class with ID {request.Id} not found.");

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == request.SubjectId, cancellationToken);

            if (!subjectExists)
            {
                throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");
            }

            if (request.LecturerId.HasValue)
            {
                var lecturer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.LecturerId.Value, cancellationToken);

                if (lecturer == null)
                {
                    throw new KeyNotFoundException($"Lecturer with ID {request.LecturerId.Value} not found.");
                }

                if (lecturer.Role != "Lecturer")
                {
                    throw new ArgumentException("Selected user is not a lecturer.");
                }
            }

            var duplicate = await _context.ExamClasses
                .AnyAsync(ec =>
                    ec.Id != request.Id &&
                    ec.ClassCode == classCode &&
                    ec.Semester == semester,
                    cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException($"Exam class '{classCode}' already exists in semester '{semester}'.");
            }

            examClass.ClassCode = classCode;
            examClass.SubjectId = request.SubjectId;
            examClass.Semester = semester;
            examClass.LecturerId = request.LecturerId;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                examClass.Status = request.Status.Trim();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}