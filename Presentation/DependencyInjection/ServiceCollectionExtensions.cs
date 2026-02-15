using Microsoft.Extensions.DependencyInjection;

namespace PicoPlus.Presentation.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        // Place presentation-specific registration here as migration progresses.
        return services;
    }
}
