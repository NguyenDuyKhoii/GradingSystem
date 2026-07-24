using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.AuthService.Grpc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.GrpcClients
{
    public class UserGrpcClient : IUserGrpcClient
    {
        private readonly UserSyncService.UserSyncServiceClient _client;
        private readonly IApplicationDbContext _context;

        public UserGrpcClient(UserSyncService.UserSyncServiceClient client, IApplicationDbContext context)
        {
            _client = client;
            _context = context;
        }

        public async Task EnsureUserExistsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (exists) return;

            try
            {
                var userResponse = await _client.GetUserByIdAsync(new GetUserRequest { Id = userId }, cancellationToken: cancellationToken);
                if (userResponse != null)
                {
                    _context.Users.Add(new Domain.Entities.User
                    {
                        Id = userResponse.Id,
                        Username = userResponse.Username,
                        Role = userResponse.Role,
                        Email = userResponse.Email,
                        FullName = userResponse.FullName,
                        PasswordHash = string.Empty
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[gRPC UserGrpcClient Error] Failed to fetch user {userId} via gRPC: {ex.Message}");
            }
        }
    }
}
