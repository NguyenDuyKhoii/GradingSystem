using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using FptuGradingSystem.AuthService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Commands
{
    public record LoginCommand(
        string Username,
        string Password,
        string? IpAddress = null,
        string? UserAgent = null) : IRequest<AuthResponse>;

    public record AuthResponse(
        int Id,
        string Username,
        string Token,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        string Role,
        string FullName);

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IAuthDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(
            IAuthDbContext context, 
            IPasswordHasher passwordHasher, 
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            var (token, jti, tokenExpiresAt) = _jwtTokenGenerator.GenerateToken(user);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                JwtId = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                user.Id,
                user.Username,
                token,
                refreshTokenString,
                refreshToken.ExpiresAt,
                user.Role,
                user.FullName
            );
        }
    }
}
