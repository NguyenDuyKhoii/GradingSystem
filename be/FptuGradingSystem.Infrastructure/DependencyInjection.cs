using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Infrastructure.Data;
using FptuGradingSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FptuGradingSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IJwtTokenGenerator, Authentication.JwtTokenGenerator>();
            services.AddScoped<IPasswordHasher, Authentication.PasswordHasher>();

            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
            services.AddScoped<IZipProcessingQueue, RedisZipProcessingQueue>();

            return services;
        }
    }
}
