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
        string ClassCode,
        int SubjectId,
        string Semester,
        int? LecturerId) : IRequest<int>;

    public class CreateExamClassCommandHandler : IRequestHandler<CreateExamClassCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateExamClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateExamClassCommand request, CancellationToken cancellationToken)
        {
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

                if (lecturer == null || lecturer.Role != "Lecturer")
                {
                    throw new ArgumentException("Lecturer not found or user is not a lecturer.");
                }
            }

            var examClass = new ExamClass
            {
                ClassCode = request.ClassCode,
                SubjectId = request.SubjectId,
                Semester = request.Semester,
                LecturerId = request.LecturerId,
                Status = "Pending"
            };

            _context.ExamClasses.Add(examClass);
            await _context.SaveChangesAsync(cancellationToken);

            return examClass.Id;
        }
    }
}
