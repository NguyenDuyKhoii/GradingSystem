using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Classes.Commands
{
    public record UpdateClassCommand(int Id, string ClassCode) : IRequest;

    public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateClassCommand request, CancellationToken cancellationToken)
        {
            var classCode = request.ClassCode.Trim();

            if (string.IsNullOrWhiteSpace(classCode))
            {
                throw new ArgumentException("Class code is required.");
            }

            var entity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Class with ID {request.Id} not found.");

            var duplicate = await _context.Classes
                .AnyAsync(c => c.Id != request.Id && c.ClassCode == classCode, cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException($"Class '{classCode}' already exists.");
            }

            entity.ClassCode = classCode;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
