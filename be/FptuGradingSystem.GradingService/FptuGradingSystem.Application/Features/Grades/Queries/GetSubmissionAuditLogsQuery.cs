using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Grades.Queries
{
    public record GetSubmissionAuditLogsQuery(int SubmissionId) : IRequest<List<GradeAuditLogDto>>;

    public record GradeAuditLogDto(
        int Id,
        int SubmissionId,
        int? GradeId,
        int? RubricCriteriaId,
        string CriteriaName,
        decimal OldScore,
        decimal NewScore,
        string? OldFeedback,
        string? NewFeedback,
        int ChangedById,
        string ChangedByName,
        string ChangedByRole,
        string Reason,
        DateTime ChangedAt);

    public class GetSubmissionAuditLogsQueryHandler : IRequestHandler<GetSubmissionAuditLogsQuery, List<GradeAuditLogDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetSubmissionAuditLogsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GradeAuditLogDto>> Handle(GetSubmissionAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var logs = await _context.GradeAuditLogs
                .AsNoTracking()
                .Where(l => l.SubmissionId == request.SubmissionId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync(cancellationToken);

            return logs.Select(l => new GradeAuditLogDto(
                l.Id,
                l.SubmissionId,
                l.GradeId,
                l.RubricCriteriaId,
                l.CriteriaName,
                l.OldScore,
                l.NewScore,
                l.OldFeedback,
                l.NewFeedback,
                l.ChangedById,
                l.ChangedByName,
                l.ChangedByRole,
                l.Reason,
                l.ChangedAt
            )).ToList();
        }
    }
}
