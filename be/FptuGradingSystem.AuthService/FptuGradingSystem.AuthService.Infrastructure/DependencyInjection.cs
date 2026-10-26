using FptuGradingSystem.AuthService.Application.Common.Interfaces;
using FptuGradingSystem.AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FptuGradingSystem.AuthService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuthInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IAuthDbContext>(provider => provider.GetRequiredService<AuthDbContext>());
            services.AddScoped<IJwtTokenGenerator, Authentication.JwtTokenGenerator>();
            services.AddScoped<IPasswordHasher, Authentication.PasswordHasher>();

            // Redis & Token Blacklist Service
            var redisConn = configuration["Redis:ConnectionString"] ?? configuration.GetConnectionString("Redis") ?? "localhost:6379";
            try
            {
                var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn);
                services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(redis);
            }
            catch
            {
                // Graceful fallback if Redis is unreachable during startup
            }

            services.AddScoped<ITokenBlacklistService, Services.RedisTokenBlacklistService>();

            return services;
        }
    }
}
