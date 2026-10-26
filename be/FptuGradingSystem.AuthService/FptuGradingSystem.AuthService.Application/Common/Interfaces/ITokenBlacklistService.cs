using System;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.Application.Common.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string jti, TimeSpan timeToLive, string reason);
        Task<bool> IsTokenBlacklistedAsync(string jti);
    }
}
