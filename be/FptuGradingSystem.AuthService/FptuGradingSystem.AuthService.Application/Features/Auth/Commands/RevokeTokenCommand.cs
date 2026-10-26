using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Commands
{
    public record RevokeTokenCommand(
        string? RefreshToken,
        string? AccessTokenJti,
        int? UserId = null,
        string? IpAddress = null,
        string Reason = "User logout") : IRequest<bool>;

    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
    {
        private readonly IAuthDbContext _context;
        private readonly ITokenBlacklistService _blacklistService;

        public RevokeTokenCommandHandler(
            IAuthDbContext context,
            ITokenBlacklistService blacklistService)
        {
            _context = context;
            _blacklistService = blacklistService;
        }

        public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. Blacklist specific Access Token in Redis
            if (!string.IsNullOrWhiteSpace(request.AccessTokenJti))
            {
                await _blacklistService.BlacklistTokenAsync(
                    request.AccessTokenJti,
                    TimeSpan.FromMinutes(120),
                    request.Reason
                );
            }

            // 2. Revoke specific Refresh Token
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

                if (token != null && !token.IsRevoked)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = request.IpAddress;
                    token.ReasonRevoked = request.Reason;

                    if (!string.IsNullOrWhiteSpace(token.JwtId))
                    {
                        await _blacklistService.BlacklistTokenAsync(
                            token.JwtId,
                            TimeSpan.FromMinutes(120),
                            request.Reason
                        );
                    }
                }
            }
            // 3. If UserId is given without specific token, revoke all active sessions (e.g. password change / logout all)
            else if (request.UserId.HasValue)
            {
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == request.UserId.Value && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = request.IpAddress;
                    token.ReasonRevoked = request.Reason;

                    if (!string.IsNullOrWhiteSpace(token.JwtId))
                    {
                        await _blacklistService.BlacklistTokenAsync(
                            token.JwtId,
                            TimeSpan.FromMinutes(120),
                            request.Reason
                        );
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
