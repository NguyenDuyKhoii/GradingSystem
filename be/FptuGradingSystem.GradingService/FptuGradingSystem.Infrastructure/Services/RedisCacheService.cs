using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                var data = await db.StringGetAsync(key);

                if (data.IsNullOrEmpty)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(data.ToString(), _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read cache for key: {CacheKey}. Falling back to database.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var ttl = expiration ?? TimeSpan.FromMinutes(30);

                await db.StringSetAsync(key, json, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write cache for key: {CacheKey}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache for key: {CacheKey}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                var db = _redis.GetDatabase();

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    if (!server.IsConnected) continue;

                    var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                    if (keys.Length > 0)
                    {
                        await db.KeyDeleteAsync(keys);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache by prefix: {Prefix}", prefix);
            }
        }
    }
}
