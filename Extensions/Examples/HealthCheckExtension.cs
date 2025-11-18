#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions.Examples;

/// <summary>
/// Example extension demonstrating how to create a custom extension.
/// This extension adds a health check endpoint to the application.
/// </summary>
public class HealthCheckExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "health-check",
        Name = "Health Check Extension",
        Description = "Adds health check endpoints to the application",
        Version = "1.0.0",
        Author = "PicoPlus Team",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add health checks
        services.AddHealthChecks();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        // Map health check endpoint
        app.MapHealthChecks("/health");
        
        var logger = app.Services.GetRequiredService<ILogger<HealthCheckExtension>>();
        logger.LogInformation("Health check endpoint configured at /health");
    }
}
