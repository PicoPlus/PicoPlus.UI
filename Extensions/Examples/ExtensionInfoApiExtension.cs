#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions.Examples;

/// <summary>
/// Example API extension demonstrating endpoint registration.
/// Adds a simple API endpoint that returns extension information.
/// </summary>
public class ExtensionInfoApiExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "extension-info-api",
        Name = "Extension Info API",
        Description = "Provides an API endpoint to list all loaded extensions",
        Version = "1.0.0",
        Author = "PicoPlus Team",
        EnabledByDefault = true
    };

    public override void ConfigureApplication(WebApplication app)
    {
        // Get the extension manager from services
        var extensionManager = app.Services.GetService<ExtensionManager>();

        if (extensionManager != null)
        {
            // Add an endpoint that returns information about all loaded extensions
            app.MapGet("/api/extensions", () =>
            {
                var extensions = extensionManager.Extensions
                    .Select(e => new
                    {
                        e.Metadata.Id,
                        e.Metadata.Name,
                        e.Metadata.Description,
                        e.Metadata.Version,
                        e.Metadata.Author,
                        e.Metadata.Dependencies
                    })
                    .ToList();

                return Results.Ok(new
                {
                    Count = extensions.Count,
                    Extensions = extensions
                });
            })
            .WithName("GetExtensions")
            .WithTags("Extensions")
            .Produces<object>(StatusCodes.Status200OK);

            var logger = app.Services.GetRequiredService<ILogger<ExtensionInfoApiExtension>>();
            logger.LogInformation("Extension Info API endpoint configured at /api/extensions");
        }
    }
}
