using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Application.Abstractions;
using PicoPlus.Infrastructure.Persistence;
using PicoPlus.Infrastructure.State;

namespace PicoPlus.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileRepository, InMemoryUserProfileRepository>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        return services;
    }
}
