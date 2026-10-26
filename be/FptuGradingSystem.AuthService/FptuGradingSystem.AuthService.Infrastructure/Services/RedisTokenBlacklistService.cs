using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace FptuGradingSystem.AuthService.Infrastructure.Services
{
    public class RedisTokenBlacklistService : ITokenBlacklistService
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<RedisTokenBlacklistService> _logger;
        private const string BlacklistKeyPrefix = "auth:blacklist:jti:";

        public RedisTokenBlacklistService(
            ILogger<RedisTokenBlacklistService> logger,
            IConnectionMultiplexer? redis = null)
        {
            _logger = logger;
            _redis = redis;
        }

        public async Task BlacklistTokenAsync(string jti, TimeSpan timeToLive, string reason)
        {
            if (string.IsNullOrWhiteSpace(jti)) return;

            if (_redis == null || !_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Skipping token blacklist for JTI: {Jti}", jti);
                return;
            }

            try
            {
                var db = _redis.GetDatabase();
                var key = $"{BlacklistKeyPrefix}{jti}";

                // Ensure TTL is positive
                if (timeToLive <= TimeSpan.Zero)
                {
                    timeToLive = TimeSpan.FromMinutes(1);
                }

                await db.StringSetAsync(key, reason, timeToLive);
                _logger.LogInformation("Token JTI {Jti} blacklisted successfully. TTL: {Ttl}, Reason: {Reason}", jti, timeToLive, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to blacklist token JTI: {Jti}", jti);
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti)) return false;

            if (_redis == null || !_redis.IsConnected)
            {
                return false;
            }

            try
            {
                var db = _redis.GetDatabase();
                var key = $"{BlacklistKeyPrefix}{jti}";
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check token blacklist for JTI: {Jti}", jti);
                return false;
            }
        }
    }
}
