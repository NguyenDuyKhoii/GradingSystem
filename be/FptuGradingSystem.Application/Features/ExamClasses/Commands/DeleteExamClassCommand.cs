using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record DeleteExamClassCommand(int Id) : IRequest;

    public class DeleteExamClassCommandHandler : IRequestHandler<DeleteExamClassCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteExamClassCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteExamClassCommand request, CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Submissions)
                .FirstOrDefaultAsync(ec => ec.Id == request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Exam class with ID {request.Id} not found.");

            if (examClass.Submissions.Count > 0)
            {
                throw new ArgumentException("Cannot delete exam class that already has submissions.");
            }

            _context.ExamClasses.Remove(examClass);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}