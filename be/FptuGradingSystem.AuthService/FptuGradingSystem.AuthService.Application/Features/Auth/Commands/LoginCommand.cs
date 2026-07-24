using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FptuGradingSystem.AuthService.Application.Features.Auth.Commands
{
    public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;

    public record AuthResponse(
        int Id,
        string Username,
        string Token,
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
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResponse(
                user.Id,
                user.Username,
                token,
                user.Role,
                user.FullName
            );
        }
    }
}
