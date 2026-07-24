using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using FptuGradingSystem.AuthService.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.API.Services
{
    public class UserSyncServiceImpl : UserSyncService.UserSyncServiceBase
    {
        private readonly IAuthDbContext _context;

        public UserSyncServiceImpl(IAuthDbContext context)
        {
            _context = context;
        }

        public override async Task<UserResponse> GetUserById(GetUserRequest request, ServerCallContext context)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {request.Id} not found in AuthService."));
            }

            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                Role = user.Role ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };
        }

        public override async Task<UserListResponse> GetAllUsers(EmptyRequest request, ServerCallContext context)
        {
            var users = await _context.Users.ToListAsync();
            var response = new UserListResponse();

            foreach (var user in users)
            {
                response.Users.Add(new UserResponse
                {
                    Id = user.Id,
                    Username = user.Username ?? string.Empty,
                    Role = user.Role ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty
                });
            }

            return response;
        }
    }
}
