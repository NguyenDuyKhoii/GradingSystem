using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Subjects.Commands
{
    public record UpdateSubjectCommand(int Id, string SubjectCode, string SubjectName) : IRequest<Unit>;

    public class UpdateSubjectCommandHandler : IRequestHandler<UpdateSubjectCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public UpdateSubjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
        {
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Subject with ID {request.Id} not found.");

            var duplicate = await _context.Subjects
                .AnyAsync(s => s.SubjectCode == request.SubjectCode && s.Id != request.Id, cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException($"Subject with code '{request.SubjectCode}' already exists.");
            }

            subject.SubjectCode = request.SubjectCode;
            subject.SubjectName = request.SubjectName;

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
