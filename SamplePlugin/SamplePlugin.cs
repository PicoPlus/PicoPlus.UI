#nullable enable

using PicoPlus.Infrastructure.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace SamplePlugin;

/// <summary>
/// Sample plugin demonstrating the PicoPlus plugin system.
/// This plugin shows how to:
/// - Implement the IPlugin interface
/// - Provide metadata
/// - Register services
/// - Handle lifecycle events
/// </summary>
public class SamplePlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "sample-plugin",
        Name = "Sample Plugin",
        Version = "1.0.0",
        Description = "A sample plugin demonstrating the PicoPlus plugin architecture",
        Author = "PicoPlus Team",
        Website = "https://github.com/PicoPlus/PicoPlus.UI",
        License = "MIT",
        Tags = new[] { "sample", "demo", "example" },
        MinimumPicoPlusVersion = "1.0.0"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
        logger.LogInformation("Sample Plugin is loading...");

        // Register plugin services
        services.AddScoped<ISampleService, SampleService>();
        
        logger.LogInformation("Sample Plugin registered services successfully");

        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
        logger.LogInformation("Sample Plugin is unloading...");

        // Clean up resources if needed

        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
        logger.LogInformation("Sample Plugin is being enabled");

        // Perform any initialization when plugin is activated

        return Task.CompletedTask;
    }

    public Task OnDisableAsync(PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
        logger.LogInformation("Sample Plugin is being disabled");

        // Perform any cleanup when plugin is deactivated

        return Task.CompletedTask;
    }
}

/// <summary>
/// Sample service interface provided by the plugin.
/// </summary>
public interface ISampleService
{
    string GetMessage();
}

/// <summary>
/// Sample service implementation.
/// </summary>
public class SampleService : ISampleService
{
    public string GetMessage()
    {
        return "Hello from Sample Plugin!";
    }
}
