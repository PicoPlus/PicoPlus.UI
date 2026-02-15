using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Application.Abstractions;
using PicoPlus.Infrastructure.Persistence;

namespace PicoPlus.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileRepository, InMemoryUserProfileRepository>();
        return services;
    }
}
