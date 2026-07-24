using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Submissions.Queries
{
    public record GetSubmissionsQuery(
        int ExamClassId,
        string? SearchQuery = null,
        string? Status = null,
        int PageNumber = 1,
        int PageSize = 10) : IRequest<PaginatedList<SubmissionDto>>;

    public record SubmissionDto(
        int Id,
        int ExamClassId,
        string StudentId,
        string StudentName,
        string FilePath,
        string FileType,
        string Status,
        decimal? TotalScore);

    public class GetSubmissionsQueryHandler : IRequestHandler<GetSubmissionsQuery, PaginatedList<SubmissionDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetSubmissionsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<SubmissionDto>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Submissions
                .Include(s => s.Grade)
                .Where(s => s.ExamClassId == request.ExamClassId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchQuery))
            {
                var search = request.SearchQuery.ToLower();
                query = query.Where(s => s.StudentId.ToLower().Contains(search) || 
                                         s.StudentName.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(s => s.Status == request.Status);
            }

            var dtoListQuery = query.Select(s => new SubmissionDto(
                s.Id,
                s.ExamClassId,
                s.StudentId,
                s.StudentName,
                s.FilePath,
                s.FileType,
                s.Status,
                s.Grade != null ? s.Grade.TotalScore : null
            ));

            return await PaginatedList<SubmissionDto>.CreateAsync(
                dtoListQuery, 
                request.PageNumber, 
                request.PageSize, 
                cancellationToken);
        }
    }
}
