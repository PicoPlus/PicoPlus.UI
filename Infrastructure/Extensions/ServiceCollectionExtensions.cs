#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add extension support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the extension system to the service collection and discovers/configures all extensions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureOptions">Optional configuration for extension options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExtensions(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<ExtensionOptions>? configureOptions = null)
    {
        // Configure options
        var options = new ExtensionOptions();
        configureOptions?.Invoke(options);

        // Create a temporary service provider to get the logger
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ExtensionManager>();

        // Create extension manager and discover extensions
        var extensionManager = new ExtensionManager(logger, options);
        
        if (options.EnableDiscovery)
        {
            extensionManager.DiscoverExtensions();
        }

        // Configure services for all extensions
        extensionManager.ConfigureServices(services, configuration);

        // Register the extension manager as a singleton
        services.AddSingleton(extensionManager);

        return services;
    }
}

/// <summary>
/// Extension methods for WebApplication to configure extensions.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the application pipeline for all loaded extensions.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseExtensions(this WebApplication app)
    {
        var extensionManager = app.Services.GetService<ExtensionManager>();
        
        if (extensionManager != null)
        {
            extensionManager.ConfigureApplication(app);
        }
        else
        {
            var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
            logger.LogWarning("ExtensionManager not found. Make sure to call AddExtensions() in Program.cs");
        }

        return app;
    }
}
