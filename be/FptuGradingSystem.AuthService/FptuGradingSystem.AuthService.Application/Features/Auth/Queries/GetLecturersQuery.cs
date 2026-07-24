using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Queries
{
    public record GetLecturersQuery : IRequest<List<LecturerDto>>;

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
            return await _context.Users
                .Where(u => u.Role == "Lecturer")
                .Select(u => new LecturerDto(u.Id, u.FullName, u.Username, u.Email))
                .ToListAsync(cancellationToken);
        }
    }
}
