using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Subjects.Queries
{
    public record GetSubjectsQuery(
        string? SearchTerm = null,
        string? SortBy = null,
        bool IsDescending = false,
        int PageNumber = 1,
        int PageSize = 10) : IRequest<PaginatedList<SubjectDto>>;

    public record SubjectDto(int Id, string SubjectCode, string SubjectName);

    public class GetSubjectsQueryHandler : IRequestHandler<GetSubjectsQuery, PaginatedList<SubjectDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public GetSubjectsQueryHandler(IApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<PaginatedList<SubjectDto>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"subjects:p:{request.PageNumber}:s:{request.PageSize}:q:{request.SearchTerm}:sort:{request.SortBy}:desc:{request.IsDescending}";

            var cached = await _cacheService.GetAsync<PaginatedList<SubjectDto>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            var query = _context.Subjects
                .AsNoTracking()
                .AsQueryable();

            // Search by SubjectCode or SubjectName
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(s => s.SubjectCode.ToLower().Contains(term) ||
                                         s.SubjectName.ToLower().Contains(term));
            }

            // Dynamic Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var sortByLower = request.SortBy.ToLower();
                if (sortByLower == "code" || sortByLower == "subjectcode")
                {
                    query = request.IsDescending ? query.OrderByDescending(s => s.SubjectCode) : query.OrderBy(s => s.SubjectCode);
                }
                else if (sortByLower == "name" || sortByLower == "subjectname")
                {
                    query = request.IsDescending ? query.OrderByDescending(s => s.SubjectName) : query.OrderBy(s => s.SubjectName);
                }
                else
                {
                    query = query.OrderBy(s => s.Id);
                }
            }
            else
            {
                query = query.OrderBy(s => s.Id);
            }

            var dtoQuery = query.Select(s => new SubjectDto(s.Id, s.SubjectCode, s.SubjectName));

            var result = await PaginatedList<SubjectDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize, cancellationToken);

            await _cacheService.SetAsync(cacheKey, result, System.TimeSpan.FromMinutes(15), cancellationToken);

            return result;
        }
    }
}
