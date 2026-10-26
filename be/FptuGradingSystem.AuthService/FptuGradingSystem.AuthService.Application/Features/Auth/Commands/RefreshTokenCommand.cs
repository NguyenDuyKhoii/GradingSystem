using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using FptuGradingSystem.AuthService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Commands
{
    public record RefreshTokenCommand(
        string RefreshToken,
        string? IpAddress = null,
        string? UserAgent = null) : IRequest<AuthResponse>;

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IAuthDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ITokenBlacklistService _blacklistService;

        public RefreshTokenCommandHandler(
            IAuthDbContext context,
            IJwtTokenGenerator jwtTokenGenerator,
            ITokenBlacklistService blacklistService)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _blacklistService = blacklistService;
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (existingToken == null)
            {
                throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
            }

            // Token Compromise / Replay Attack Detection
            if (existingToken.IsRevoked)
            {
                // Revoke all ancestor/descendant active tokens for this user to protect against token theft
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                foreach (var activeToken in activeTokens)
                {
                    activeToken.RevokedAt = DateTime.UtcNow;
                    activeToken.RevokedByIp = request.IpAddress;
                    activeToken.ReasonRevoked = "Nghi vấn xâm phạm (phát hiện tái sử dụng token đã thu hồi)";

                    if (!string.IsNullOrWhiteSpace(activeToken.JwtId))
                    {
                        await _blacklistService.BlacklistTokenAsync(activeToken.JwtId, TimeSpan.FromMinutes(120), "Compromise detection");
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedAccessException("Phát hiện bất thường: Refresh token đã bị thu hồi trước đó. Toàn bộ phiên đăng nhập đã bị hủy để đảm bảo an toàn.");
            }

            if (existingToken.IsExpired)
            {
                throw new UnauthorizedAccessException("Refresh token đã hết hạn, vui lòng đăng nhập lại.");
            }

            if (existingToken.User == null)
            {
                throw new UnauthorizedAccessException("Tài khoản người dùng không hợp lệ.");
            }

            // Blacklist the old access token JTI in Redis
            if (!string.IsNullOrWhiteSpace(existingToken.JwtId))
            {
                await _blacklistService.BlacklistTokenAsync(existingToken.JwtId, TimeSpan.FromMinutes(120), "Token rotated");
            }

            // Generate new Access Token + Refresh Token
            var (newAccessToken, newJti, newExpiresAt) = _jwtTokenGenerator.GenerateToken(existingToken.User);
            var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            // Mark old refresh token as rotated/revoked
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = request.IpAddress;
            existingToken.ReplacedByToken = newRefreshTokenString;
            existingToken.ReasonRevoked = "Replaced by token rotation";

            var newRefreshToken = new RefreshToken
            {
                UserId = existingToken.UserId,
                Token = newRefreshTokenString,
                JwtId = newJti,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                existingToken.User.Id,
                existingToken.User.Username,
                newAccessToken,
                newRefreshTokenString,
                newRefreshToken.ExpiresAt,
                existingToken.User.Role,
                existingToken.User.FullName
            );
        }
    }
}
