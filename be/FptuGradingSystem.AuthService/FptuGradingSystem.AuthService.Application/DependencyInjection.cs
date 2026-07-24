using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FptuGradingSystem.AuthService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuthApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
