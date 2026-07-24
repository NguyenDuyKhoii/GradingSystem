using FptuGradingSystem.AuthService.Domain.Entities;

namespace FptuGradingSystem.AuthService.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
