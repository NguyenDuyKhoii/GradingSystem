using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Classes.Commands
{
    public record DeleteClassCommand(int Id) : IRequest;

    public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Classes
                .Include(c => c.ExamClasses)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Class with ID {request.Id} not found.");

            // Avoid deleting class if it has associated exam classes
            if (entity.ExamClasses.Count > 0)
            {
                throw new System.InvalidOperationException("Cannot delete class because it has associated exam classes.");
            }

            _context.Classes.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
