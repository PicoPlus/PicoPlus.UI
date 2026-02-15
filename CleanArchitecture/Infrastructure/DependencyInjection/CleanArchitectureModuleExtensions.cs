using Microsoft.Extensions.DependencyInjection;
using PicoPlus.CleanArchitecture.Application.UseCases.Auth;
using PicoPlus.CleanArchitecture.InterfaceAdapters.Auth;

namespace PicoPlus.CleanArchitecture.Infrastructure.DependencyInjection;

public static class CleanArchitectureModuleExtensions
{
    public static IServiceCollection AddCleanArchitectureCore(this IServiceCollection services)
    {
        services.AddScoped<ILoginByNationalCodeUseCase, LoginByNationalCodeUseCase>();
        services.AddScoped<LoginViewModelAdapter>();
        return services;
    }
}
