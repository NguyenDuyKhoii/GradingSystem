using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Auth.Queries
{
    public record GetLecturersQuery : IRequest<List<LecturerDto>>;

    public record LecturerDto(int Id, string FullName, string Username, string Email);

    public class GetLecturersQueryHandler : IRequestHandler<GetLecturersQuery, List<LecturerDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetLecturersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LecturerDto>> Handle(GetLecturersQuery request, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Where(u => u.Role == "Lecturer")
                .Select(u => new LecturerDto(u.Id, u.FullName, u.Username, u.Email))
                .ToListAsync(cancellationToken);
        }
    }
}
