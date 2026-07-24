using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Subjects.Commands
{
    public record DeleteSubjectCommand(int Id) : IRequest<Unit>;

    public class DeleteSubjectCommandHandler : IRequestHandler<DeleteSubjectCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteSubjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteSubjectCommand request, CancellationToken cancellationToken)
        {
            var subject = await _context.Subjects
                .Include(s => s.Rubrics)
                .Include(s => s.ExamClasses)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Subject with ID {request.Id} not found.");

            if (subject.Rubrics.Count > 0)
            {
                throw new ArgumentException("Cannot delete subject that has associated rubrics. Remove the rubrics first.");
            }

            if (subject.ExamClasses.Count > 0)
            {
                throw new ArgumentException("Cannot delete subject that has associated exam classes. Remove the exam classes first.");
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
