using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Queries
{
    public record GetLecturersQuery(
        string? SearchTerm = null,
        string? SortBy = null,
        bool IsDescending = false) : IRequest<List<LecturerDto>>;

    public record LecturerDto(int Id, string FullName, string Username, string Email);

    public class GetLecturersQueryHandler : IRequestHandler<GetLecturersQuery, List<LecturerDto>>
    {
        private readonly IAuthDbContext _context;

        public GetLecturersQueryHandler(IAuthDbContext context)
        {
            _context = context;
        }

        public async Task<List<LecturerDto>> Handle(GetLecturersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Lecturer")
                .AsQueryable();

            // Search by FullName, Username, or Email
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) ||
                                         u.Username.ToLower().Contains(term) ||
                                         u.Email.ToLower().Contains(term));
            }

            // Dynamic Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var sortByLower = request.SortBy.ToLower();
                if (sortByLower == "name" || sortByLower == "fullname")
                {
                    query = request.IsDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName);
                }
                else if (sortByLower == "username")
                {
                    query = request.IsDescending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username);
                }
                else if (sortByLower == "email")
                {
                    query = request.IsDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                }
                else
                {
                    query = query.OrderBy(u => u.Id);
                }
            }
            else
            {
                query = query.OrderBy(u => u.FullName);
            }

            return await query
                .Select(u => new LecturerDto(u.Id, u.FullName, u.Username, u.Email))
                .ToListAsync(cancellationToken);
        }
    }
}
