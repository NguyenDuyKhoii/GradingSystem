using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Classes.Queries
{
    public record GetClassesQuery(
        string? SearchTerm = null,
        string? SortBy = null,
        bool IsDescending = false,
        int PageNumber = 1,
        int PageSize = 10) : IRequest<PaginatedList<ClassDto>>;

    public record ClassDto(int Id, string ClassCode);

    public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, PaginatedList<ClassDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClassesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Classes
                .AsNoTracking()
                .AsQueryable();

            // Search by ClassCode
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(c => c.ClassCode.ToLower().Contains(term));
            }

            // Dynamic Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var sortByLower = request.SortBy.ToLower();
                if (sortByLower == "code" || sortByLower == "classcode")
                {
                    query = request.IsDescending ? query.OrderByDescending(c => c.ClassCode) : query.OrderBy(c => c.ClassCode);
                }
                else
                {
                    query = query.OrderBy(c => c.Id);
                }
            }
            else
            {
                query = query.OrderBy(c => c.ClassCode);
            }

            var dtoQuery = query.Select(c => new ClassDto(c.Id, c.ClassCode));

            return await PaginatedList<ClassDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
