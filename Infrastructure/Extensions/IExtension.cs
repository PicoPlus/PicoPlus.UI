#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Base interface for all extensions in the PicoPlus system.
/// Extensions can register services and configure the application pipeline.
/// </summary>
public interface IExtension
{
    /// <summary>
    /// Gets the metadata information about this extension.
    /// </summary>
    ExtensionMetadata Metadata { get; }

    /// <summary>
    /// Called during application startup to register services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Called during application startup to configure the application pipeline.
    /// This is called after ConfigureServices.
    /// </summary>
    /// <param name="app">The application builder.</param>
    void ConfigureApplication(WebApplication app);
}
