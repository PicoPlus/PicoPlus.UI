# Extension System - Quick Start Guide

## What is the Extension System?

The Extension System allows you to add new features to PicoPlus.UI in a modular way without modifying the core codebase. Each extension is a self-contained unit that can register services and configure the application pipeline.

## Quick Example

Here's a complete example of creating a simple extension:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions.MyFeature;

public class MyFeatureExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "my-feature",
        Name = "My Feature",
        Description = "Adds a custom feature",
        Version = "1.0.0",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register your services
        services.AddScoped<IMyService, MyService>();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        // Configure endpoints or middleware
        app.MapGet("/my-feature-endpoint", () => "Hello from my feature!");
    }
}
```

## How It Works

1. **Discovery**: The system automatically discovers all classes that implement `IExtension`
2. **Registration**: Extensions are registered in dependency order
3. **Configuration**: Each extension's `ConfigureServices` and `ConfigureApplication` methods are called

## Included Example Extensions

### Health Check Extension (Enabled by Default)

Adds a health check endpoint at `/health`:

- **File**: `Extensions/Examples/HealthCheckExtension.cs`
- **Status**: Enabled by default
- **Usage**: Navigate to `/health` to see the health status

### Custom Logging Extension (Disabled by Default)

Demonstrates service registration:

- **File**: `Extensions/Examples/CustomLoggingExtension.cs`
- **Status**: Disabled by default (for demonstration)
- **Enable**: Add `options.EnabledExtensions.Add("custom-logging");` in Program.cs

## Creating Your Own Extension

1. **Create a new class** in `Extensions/` folder or subdirectory
2. **Inherit from `BaseExtension`** or implement `IExtension`
3. **Override `Metadata`** property with your extension information
4. **Override `ConfigureServices`** to register services (optional)
5. **Override `ConfigureApplication`** to configure the pipeline (optional)

## Controlling Extensions

In `Program.cs`, you can control which extensions are loaded:

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    // Disable an extension
    options.DisabledExtensions.Add("health-check");
    
    // Enable a disabled extension
    options.EnabledExtensions.Add("custom-logging");
});
```

## Where to Put Your Extensions

- **Simple extensions**: Place directly in `Extensions/` folder
- **Complex features**: Create a subfolder like `Extensions/MyFeature/`
- **Examples**: Keep in `Extensions/Examples/` folder

## Common Use Cases

- **API Endpoints**: Add new REST API endpoints
- **Background Services**: Register hosted services
- **Middleware**: Add custom middleware to the pipeline
- **External Integrations**: Connect to external services
- **Feature Toggles**: Enable/disable features based on configuration
- **Authentication Providers**: Add new authentication methods
- **Logging & Monitoring**: Add custom logging or monitoring tools

## Need More Information?

See the complete documentation at `Infrastructure/Extensions/README.md`

## Testing Your Extension

You can test if your extension is loaded by:

1. Running the application
2. Checking the logs for "Loaded extension: [Your Extension Name]"
3. Testing the functionality your extension provides

## Tips

- Start with the example extensions to understand the pattern
- Use dependency injection for all your services
- Log important events in your extension
- Keep extensions focused on a single feature
- Document what your extension does in the metadata

Happy extending! 🚀
