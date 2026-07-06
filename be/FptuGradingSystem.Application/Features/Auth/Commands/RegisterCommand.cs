using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Auth.Commands
{
    public record RegisterCommand(
        string Username,
        string Password,
        string Role,
        string Email,
        string FullName) : IRequest<int>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<int> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Validate unique username
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (existingUser != null)
            {
                throw new ArgumentException("Username is already taken.");
            }

            // Validate role
            if (request.Role != "Lecturer" && request.Role != "AcademicStaff")
            {
                throw new ArgumentException("Invalid role. Role must be 'Lecturer' or 'AcademicStaff'.");
            }

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
                Role = request.Role,
                Email = request.Email,
                FullName = request.FullName
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
