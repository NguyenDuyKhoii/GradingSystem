using FptuGradingSystem.AuthService.Domain.Entities;
using System;

namespace FptuGradingSystem.AuthService.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, string Jti, DateTime ExpiresAt) GenerateToken(User user);
        string GenerateRefreshToken();
    }
}
