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
        int ClassId,
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

            var classExists = await _context.Classes
                .AnyAsync(c => c.Id == request.ClassId, cancellationToken);

            if (!classExists)
            {
                throw new KeyNotFoundException($"Class with ID {request.ClassId} not found.");
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == request.SubjectId, cancellationToken);

            if (!subjectExists)
            {
                throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");
            }

            // LecturerId is assigned directly from AuthService

            var duplicate = await _context.ExamClasses
                .AnyAsync(ec =>
                    ec.ClassId == request.ClassId &&
                    ec.SubjectId == request.SubjectId &&
                    ec.Semester == semester,
                    cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException("An exam class already exists for this class, subject, and semester.");
            }

            var examClass = new ExamClass
            {
                ClassId = request.ClassId,
                SubjectId = request.SubjectId,
                Semester = semester,
                LecturerId = request.LecturerId,
                Status = "Pending"
            };

            _context.ExamClasses.Add(examClass);
            await _context.SaveChangesAsync(cancellationToken);

            return examClass.Id;
        }
    }
}