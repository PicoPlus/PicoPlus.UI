# Extension System Documentation

## Overview

The PicoPlus Extension System provides a modular and flexible way to add new features to the application without modifying the core codebase. Extensions can register services, configure the application pipeline, and add new functionality.

## Architecture

The extension system consists of the following components:

### Core Interfaces and Classes

1. **IExtension** - Base interface that all extensions must implement
2. **BaseExtension** - Abstract base class providing default implementations
3. **ExtensionMetadata** - Metadata about an extension (ID, name, version, etc.)
4. **ExtensionManager** - Manages discovery, loading, and lifecycle of extensions
5. **ExtensionOptions** - Configuration options for the extension system

## Creating an Extension

### Step 1: Create Your Extension Class

Create a new class that inherits from `BaseExtension`:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions.MyFeature;

public class MyFeatureExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "my-feature",
        Name = "My Feature Extension",
        Description = "Adds my awesome feature",
        Version = "1.0.0",
        Author = "Your Name",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register your services here
        services.AddScoped<IMyService, MyService>();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        // Configure application pipeline here
        app.MapGet("/my-feature", () => "Hello from my feature!");
    }
}
```

### Step 2: Extension Metadata

The `ExtensionMetadata` class provides information about your extension:

- **Id**: Unique identifier (kebab-case recommended)
- **Name**: Display name of the extension
- **Description**: What the extension does
- **Version**: Semantic version (e.g., "1.0.0")
- **Author**: Creator of the extension
- **Dependencies**: Array of extension IDs this extension depends on
- **EnabledByDefault**: Whether the extension should be enabled by default

### Step 3: ConfigureServices Method

Use this method to register services with the dependency injection container:

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Register services
    services.AddScoped<IMyService, MyService>();
    
    // Register HttpClients
    services.AddHttpClient<MyApiClient>();
    
    // Configure options from configuration
    services.Configure<MyOptions>(configuration.GetSection("MyFeature"));
}
```

### Step 4: ConfigureApplication Method

Use this method to configure the application pipeline:

```csharp
public override void ConfigureApplication(WebApplication app)
{
    // Add middleware
    app.UseMiddleware<MyCustomMiddleware>();
    
    // Map endpoints
    app.MapGet("/my-endpoint", () => Results.Ok("Response"));
    
    // Access services
    var logger = app.Services.GetRequiredService<ILogger<MyFeatureExtension>>();
    logger.LogInformation("My feature extension initialized");
}
```

## Using the Extension System

### In Program.cs

Add extensions to your application in `Program.cs`:

```csharp
// Add extension system (place after builder.Services configuration)
builder.Services.AddExtensions(builder.Configuration, options =>
{
    // Optional: Disable specific extensions
    options.DisabledExtensions.Add("my-feature");
    
    // Optional: Enable specific extensions that are disabled by default
    options.EnabledExtensions.Add("custom-logging");
});

// ... other configuration ...

var app = builder.Build();

// Configure extensions (place in the middleware pipeline where appropriate)
app.UseExtensions();
```

## Extension Dependencies

If your extension depends on other extensions, specify them in the metadata:

```csharp
public override ExtensionMetadata Metadata => new()
{
    Id = "advanced-feature",
    Name = "Advanced Feature",
    Dependencies = new[] { "basic-feature", "logging-feature" }
};
```

The extension manager will ensure dependencies are loaded and configured in the correct order.

## Extension Discovery

By default, the extension system automatically discovers all extensions in the current assembly. Extensions are discovered by:

1. Scanning for types that implement `IExtension`
2. Excluding interfaces and abstract classes
3. Creating instances using the default constructor
4. Checking if the extension is enabled
5. Sorting by dependencies

## Controlling Extension Loading

### Disable an Extension

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    options.DisabledExtensions.Add("extension-id");
});
```

### Enable a Disabled Extension

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    options.EnabledExtensions.Add("extension-id");
});
```

### Disable Discovery

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    options.EnableDiscovery = false;
});
```

## Best Practices

1. **Single Responsibility**: Each extension should focus on one feature or capability
2. **Naming Convention**: Use kebab-case for extension IDs (e.g., "health-check")
3. **Error Handling**: Extensions should handle their own errors gracefully
4. **Logging**: Use ILogger for all logging needs
5. **Configuration**: Use IConfiguration for extension settings
6. **Dependencies**: Declare all extension dependencies explicitly
7. **Documentation**: Document what your extension does and any configuration options

## Examples

### Health Check Extension

See `Extensions/Examples/HealthCheckExtension.cs` for a complete example that adds health check endpoints.

### Custom Logging Extension

See `Extensions/Examples/CustomLoggingExtension.cs` for an example that registers custom services.

## Troubleshooting

### Extension Not Loading

1. Check that the extension class is public and not abstract
2. Verify the extension has a parameterless constructor
3. Check if the extension is disabled in configuration
4. Review application logs for error messages

### Services Not Registered

1. Ensure `ConfigureServices` is called
2. Verify service registration syntax
3. Check for exceptions during service configuration

### Pipeline Configuration Issues

1. Ensure `UseExtensions()` is called in the correct order in Program.cs
2. Check for middleware ordering conflicts
3. Review logs for configuration errors

## Advanced Topics

### Custom Extension Base Classes

You can create custom base classes for specific types of extensions:

```csharp
public abstract class ApiExtension : BaseExtension
{
    protected abstract string ApiPrefix { get; }
    
    public override void ConfigureApplication(WebApplication app)
    {
        ConfigureApi(app.MapGroup(ApiPrefix));
    }
    
    protected abstract void ConfigureApi(RouteGroupBuilder group);
}
```

### Extension Configuration

Use the configuration system to make extensions configurable:

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var apiKey = configuration["MyExtension:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        services.AddSingleton(new MyApiClient(apiKey));
    }
}
```

## Support

For questions or issues with the extension system, please create an issue in the repository.
