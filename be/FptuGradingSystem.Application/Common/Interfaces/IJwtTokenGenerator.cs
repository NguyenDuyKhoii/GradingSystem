using FptuGradingSystem.Domain.Entities;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
