#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions.Examples;

/// <summary>
/// Example service interface for the logging extension.
/// </summary>
public interface ICustomLoggingService
{
    void LogCustomMessage(string message);
}

/// <summary>
/// Example service implementation.
/// </summary>
public class CustomLoggingService : ICustomLoggingService
{
    private readonly ILogger<CustomLoggingService> _logger;

    public CustomLoggingService(ILogger<CustomLoggingService> logger)
    {
        _logger = logger;
    }

    public void LogCustomMessage(string message)
    {
        _logger.LogInformation("Custom Log: {Message}", message);
    }
}

/// <summary>
/// Example extension demonstrating service registration.
/// This extension registers a custom logging service.
/// </summary>
public class CustomLoggingExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "custom-logging",
        Name = "Custom Logging Extension",
        Description = "Adds custom logging capabilities to the application",
        Version = "1.0.0",
        Author = "PicoPlus Team",
        EnabledByDefault = false  // Disabled by default as it's just an example
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register custom logging service
        services.AddScoped<ICustomLoggingService, CustomLoggingService>();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<CustomLoggingExtension>>();
        logger.LogInformation("Custom logging extension initialized");
    }
}
