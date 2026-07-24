using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record CreateExamClassCommand(
        int? ClassId,
        string? ClassCode,
        int SubjectId,
        string Semester,
        int? LecturerId
    ) : IRequest<int>;

    public class CreateExamClassCommandHandler : IRequestHandler<CreateExamClassCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateExamClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateExamClassCommand request, CancellationToken cancellationToken)
        {
            var semester = request.Semester.Trim();

            if (string.IsNullOrWhiteSpace(semester))
            {
                throw new ArgumentException("Semester is required.");
            }

            Class? targetClass = null;

            if (request.ClassId.HasValue && request.ClassId.Value > 0)
            {
                targetClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == request.ClassId.Value, cancellationToken);
            }

            if (targetClass == null && !string.IsNullOrWhiteSpace(request.ClassCode))
            {
                var code = request.ClassCode.Trim().ToUpper();
                targetClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.ClassCode == code, cancellationToken);

                if (targetClass == null)
                {
                    targetClass = new Class { ClassCode = code };
                    _context.Classes.Add(targetClass);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            if (targetClass == null)
            {
                throw new KeyNotFoundException("Valid Class ID or Class Code is required.");
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == request.SubjectId, cancellationToken);

            if (!subjectExists)
            {
                throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");
            }

            var duplicate = await _context.ExamClasses
                .AnyAsync(ec =>
                    ec.ClassId == targetClass.Id &&
                    ec.SubjectId == request.SubjectId &&
                    ec.Semester == semester,
                    cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException("An exam class already exists for this class, subject, and semester.");
            }
            // Validate LecturerId exists in Users table (may not be synced from AuthDb)
            int? validLecturerId = null;
            if (request.LecturerId.HasValue)
            {
                var lecturerExists = await _context.Users
                    .AnyAsync(u => u.Id == request.LecturerId.Value, cancellationToken);
                if (lecturerExists)
                    validLecturerId = request.LecturerId.Value;
            }

            var examClass = new ExamClass
            {
                ClassId = targetClass.Id,
                SubjectId = request.SubjectId,
                Semester = semester,
                LecturerId = validLecturerId,
                Status = "Pending"
            };

            _context.ExamClasses.Add(examClass);
            await _context.SaveChangesAsync(cancellationToken);

            return examClass.Id;
        }
    }
}