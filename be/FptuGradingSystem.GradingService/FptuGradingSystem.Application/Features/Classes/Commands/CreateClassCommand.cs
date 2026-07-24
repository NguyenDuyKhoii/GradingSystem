using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Classes.Commands
{
    public record CreateClassCommand(string ClassCode) : IRequest<int>;

    public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateClassCommand request, CancellationToken cancellationToken)
        {
            var classCode = request.ClassCode.Trim();

            if (string.IsNullOrWhiteSpace(classCode))
            {
                throw new ArgumentException("Class code is required.");
            }

            var duplicate = await _context.Classes
                .AnyAsync(c => c.ClassCode == classCode, cancellationToken);

            if (duplicate)
            {
                throw new ArgumentException($"Class '{classCode}' already exists.");
            }

            var newClass = new Class
            {
                ClassCode = classCode
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync(cancellationToken);

            return newClass.Id;
        }
    }
}
