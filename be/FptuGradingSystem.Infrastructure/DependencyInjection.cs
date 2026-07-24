using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Infrastructure.Data;
using FptuGradingSystem.Infrastructure.Messaging;
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

            // Register Redis ConnectionMultiplexer
            var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnectionString));

            // Register Message Publisher (Producer)
            services.AddSingleton<IMessagePublisher, RedisMessagePublisher>();

            return services;
        }
    }
}
