using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Application.Users;

namespace PicoPlus.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<GetUserProfileUseCase>();
        return services;
    }
}
