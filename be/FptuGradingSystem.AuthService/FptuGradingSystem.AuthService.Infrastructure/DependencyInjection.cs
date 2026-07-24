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

            return services;
        }
    }
}
