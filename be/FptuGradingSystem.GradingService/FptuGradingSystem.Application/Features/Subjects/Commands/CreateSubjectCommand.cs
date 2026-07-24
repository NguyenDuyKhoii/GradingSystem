using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Subjects.Commands
{
    public record CreateSubjectCommand(string SubjectCode, string SubjectName) : IRequest<int>;

    public class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateSubjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
        {
            var existing = await _context.Subjects
                .AnyAsync(s => s.SubjectCode == request.SubjectCode, cancellationToken);

            if (existing)
            {
                throw new ArgumentException($"Subject with code '{request.SubjectCode}' already exists.");
            }

            var subject = new Subject
            {
                SubjectCode = request.SubjectCode,
                SubjectName = request.SubjectName
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync(cancellationToken);

            return subject.Id;
        }
    }
}
