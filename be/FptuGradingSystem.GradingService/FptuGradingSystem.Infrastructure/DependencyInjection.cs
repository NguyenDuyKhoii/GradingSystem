using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Infrastructure.Data;
using FptuGradingSystem.Infrastructure.Messaging;
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

            // Register Redis ConnectionMultiplexer
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;

            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));

            // Register Message Publisher (Producer)
            services.AddSingleton<IMessagePublisher, RedisMessagePublisher>();
            services.AddScoped<IZipProcessingQueue, RedisZipProcessingQueue>();

            return services;
        }
    }
}
