# Plugin System Implementation Guide

## Overview

This document provides a complete guide to the plugin/extension ecosystem added to PicoPlus.UI. The plugin system allows developers to extend the application's functionality through dynamically loaded assemblies.

## System Architecture

### Components

```
PicoPlus.UI/
├── Infrastructure/Plugins/          # Core plugin system
│   ├── IPlugin.cs                   # Plugin interface
│   ├── PluginMetadata.cs           # Plugin metadata model
│   ├── PluginContext.cs            # Runtime context for plugins
│   ├── PluginLoadContext.cs        # Assembly load context
│   ├── PluginInfo.cs               # Plugin instance tracker
│   └── PluginManager.cs            # Plugin lifecycle manager
├── Plugins/                        # Plugin DLL storage
│   ├── README.md                   # Folder documentation
│   ├── plugin-state.json           # Plugin enabled/disabled state (auto-generated)
│   └── *.dll                       # Plugin assemblies (not in git)
├── SamplePlugin/                   # Example plugin project
│   ├── SamplePlugin.csproj         # Plugin project file
│   ├── SamplePlugin.cs             # Plugin implementation
│   └── README.md                   # Plugin documentation
├── Views/Admin/PluginManagement.razor  # Management UI
└── Docs/Plugins/README.md          # Complete documentation
```

### Plugin Lifecycle

1. **Discovery**: At startup, `PluginManager.DiscoverAndLoadPluginsAsync()` scans the `Plugins/` folder for DLL files
2. **Loading**: Each DLL is loaded using a dedicated `PluginLoadContext` for isolation
3. **Registration**: The plugin's `OnLoadAsync()` is called to register services with DI
4. **Activation**: If enabled, `OnEnableAsync()` is called to activate the plugin
5. **Runtime**: Plugin services are available throughout the application
6. **Deactivation**: When disabled, `OnDisableAsync()` is called
7. **Unloading**: When unloaded, `OnUnloadAsync()` is called and the assembly context is unloaded

## Creating a Plugin

### Step 1: Create Project

```bash
dotnet new classlib -n MyPlugin -f net9.0
```

### Step 2: Reference PicoPlus

Add to `MyPlugin.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="../PicoPlus.csproj">
    <Private>false</Private>
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
</ItemGroup>
```

### Step 3: Implement IPlugin

```csharp
using PicoPlus.Infrastructure.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "my-plugin",
        Name = "My Plugin",
        Version = "1.0.0",
        Description = "Does something awesome",
        Author = "Your Name",
        License = "MIT"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger(GetType().Name);
        logger.LogInformation("Loading plugin...");

        // Register your services
        services.AddScoped<IMyService, MyService>();

        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context)
    {
        // Plugin is being enabled
        return Task.CompletedTask;
    }

    public Task OnDisableAsync(PluginContext context)
    {
        // Plugin is being disabled
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(PluginContext context)
    {
        // Plugin is being unloaded
        return Task.CompletedTask;
    }
}
```

### Step 4: Build and Deploy

```bash
dotnet build MyPlugin.csproj -c Release
cp bin/Release/net9.0/MyPlugin.dll ../Plugins/
```

### Step 5: Restart Application

The plugin will be discovered and loaded automatically.

## Plugin Management UI

Access the plugin management interface at:
```
/admin/plugins
```

Features:
- View all loaded plugins with metadata
- Enable/disable plugins
- Unload plugins from memory
- View plugin information and errors
- Monitor plugin status in real-time

## API Reference

### IPlugin Interface

```csharp
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    Task OnLoadAsync(IServiceCollection services, PluginContext context);
    Task OnEnableAsync(PluginContext context);
    Task OnDisableAsync(PluginContext context);
    Task OnUnloadAsync(PluginContext context);
}
```

### PluginMetadata

```csharp
public class PluginMetadata
{
    public required string Id { get; init; }                    // Unique identifier
    public required string Name { get; init; }                  // Display name
    public required string Version { get; init; }               // Semantic version
    public required string Description { get; init; }           // Description
    public string? Author { get; init; }                        // Author name
    public string? Website { get; init; }                       // Plugin website
    public string? License { get; init; }                       // License type
    public string[]? Tags { get; init; }                        // Tags/categories
    public string? MinimumPicoPlusVersion { get; init; }       // Min version
    public string[]? Dependencies { get; init; }               // Plugin dependencies
}
```

### PluginContext

```csharp
public class PluginContext
{
    public required IServiceProvider ServiceProvider { get; init; }
    public required IConfiguration Configuration { get; init; }
    public required IWebHostEnvironment Environment { get; init; }
    public required ILoggerFactory LoggerFactory { get; init; }
    public required string PluginPath { get; init; }
    public Dictionary<string, object> Data { get; init; }
}
```

### PluginManager Methods

```csharp
// Discover and load all plugins
Task DiscoverAndLoadPluginsAsync()

// Enable a plugin
Task<bool> EnablePluginAsync(string pluginId)

// Disable a plugin
Task<bool> DisablePluginAsync(string pluginId)

// Unload a plugin
Task<bool> UnloadPluginAsync(string pluginId)

// Get a plugin by ID
PluginInfo? GetPlugin(string pluginId)

// Get all loaded plugins
IReadOnlyDictionary<string, PluginInfo> LoadedPlugins { get; }
```

## Advanced Features

### Service Registration

Plugins can register any service with the DI container:

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    // Scoped services
    services.AddScoped<IMyService, MyService>();
    
    // Singleton services
    services.AddSingleton<IMyCache, MyCache>();
    
    // Factory registration
    services.AddScoped(sp => new MyFactory(
        sp.GetRequiredService<IConfiguration>()
    ));
    
    return Task.CompletedTask;
}
```

### Configuration Access

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    var apiKey = context.Configuration["MyPlugin:ApiKey"];
    var timeout = context.Configuration.GetValue<int>("MyPlugin:Timeout", 30);
    
    services.AddScoped<IMyService>(sp => 
        new MyService(apiKey, timeout));
    
    return Task.CompletedTask;
}
```

Add settings to `appsettings.json`:

```json
{
  "MyPlugin": {
    "ApiKey": "your-key",
    "Timeout": 60
  }
}
```

### Logging

```csharp
var logger = context.LoggerFactory.CreateLogger("MyPlugin");
logger.LogInformation("Plugin initialized");
logger.LogError(ex, "Failed to connect to API");
```

### Background Tasks

```csharp
private Timer? _timer;

public Task OnEnableAsync(PluginContext context)
{
    _timer = new Timer(
        callback: _ => DoWork(context),
        state: null,
        dueTime: TimeSpan.Zero,
        period: TimeSpan.FromMinutes(5)
    );
    return Task.CompletedTask;
}

public Task OnDisableAsync(PluginContext context)
{
    _timer?.Dispose();
    _timer = null;
    return Task.CompletedTask;
}
```

## Best Practices

### Do's ✅

- Use clear, descriptive plugin IDs and names
- Provide comprehensive metadata
- Log important events and errors
- Clean up resources in `OnDisableAsync`/`OnUnloadAsync`
- Handle exceptions gracefully
- Use dependency injection
- Follow semantic versioning
- Document your plugin's features

### Don'ts ❌

- Don't block lifecycle methods
- Don't use static state without cleanup
- Don't assume plugin load order
- Don't bundle the entire PicoPlus assembly
- Don't modify shared state unsafely
- Don't forget to dispose unmanaged resources

## Example Plugins

### Simple Service Plugin

```csharp
public class GreetingPlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "greeting-plugin",
        Name = "Greeting Plugin",
        Version = "1.0.0",
        Description = "Provides greeting services"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        services.AddScoped<IGreetingService, GreetingService>();
        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context) => Task.CompletedTask;
    public Task OnDisableAsync(PluginContext context) => Task.CompletedTask;
    public Task OnUnloadAsync(PluginContext context) => Task.CompletedTask;
}

public interface IGreetingService
{
    string Greet(string name);
}

public class GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
```

### Plugin with Configuration

```csharp
public class ApiPlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "api-plugin",
        Name = "API Integration",
        Version = "1.0.0",
        Description = "Integrates with external API"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        var apiUrl = context.Configuration["ApiPlugin:BaseUrl"];
        var apiKey = context.Configuration["ApiPlugin:ApiKey"];

        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiUrl);
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        });

        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context) => Task.CompletedTask;
    public Task OnDisableAsync(PluginContext context) => Task.CompletedTask;
    public Task OnUnloadAsync(PluginContext context) => Task.CompletedTask;
}
```

## Troubleshooting

### Plugin Not Loading

1. Check the DLL is in the `Plugins/` folder
2. Verify the plugin implements `IPlugin`
3. Check application logs for errors
4. Ensure the plugin targets .NET 9.0

### Plugin Shows Errors

1. Review error messages in the UI
2. Check application logs for stack traces
3. Verify all dependencies are available
4. Test the plugin in isolation

### Services Not Available

1. Ensure the plugin is enabled (not just loaded)
2. Verify services are registered in `OnLoadAsync`
3. Check for DI registration errors in logs

## Security Considerations

- Only load plugins from trusted sources
- Plugins run with full application permissions
- Review plugin source code before deployment
- Monitor plugin behavior in production
- Keep plugins updated

## Performance

- Plugins use isolated `AssemblyLoadContext` for better memory management
- Plugins can be unloaded to free memory
- Services are registered with appropriate lifetimes (Scoped/Singleton/Transient)
- Plugin state is persisted to avoid recomputation on startup

## Integration Points

### In Program.cs

```csharp
// Plugin System initialization
var tempServiceProvider = builder.Services.BuildServiceProvider();
var pluginManager = new PluginManager(
    builder.Services,
    tempServiceProvider,
    builder.Configuration,
    builder.Environment,
    tempServiceProvider.GetRequiredService<ILoggerFactory>()
);

builder.Services.AddSingleton(pluginManager);

var app = builder.Build();

// Load plugins after app is built
var logger = app.Services.GetRequiredService<ILogger<Program>>();
await pluginManager.DiscoverAndLoadPluginsAsync();
```

### In Admin Layout

```razor
<a href="/admin/plugins" class="nav-item">
    <i class="bi bi-puzzle me-2"></i>
    Plugin Management
</a>
```

## Future Enhancements

Potential improvements for the plugin system:

1. **Hot Reloading**: Reload plugins without restarting the application
2. **Dependency Resolution**: Automatically load plugin dependencies
3. **Version Checking**: Enforce minimum PicoPlus version requirements
4. **Plugin Marketplace**: Central repository for discovering plugins
5. **Plugin Permissions**: Fine-grained permission system for plugins
6. **Plugin Templates**: CLI tool to scaffold new plugins
7. **Plugin Testing Framework**: Unit testing support for plugins

## Resources

- [Main Plugin Documentation](../Docs/Plugins/README.md)
- [Sample Plugin](../SamplePlugin/README.md)
- [Plugin Management UI](../Views/Admin/PluginManagement.razor)
- [Plugin Infrastructure](../Infrastructure/Plugins/)

## Support

For questions or issues:
1. Check this documentation
2. Review the sample plugin
3. Check application logs
4. File an issue on GitHub

---

**Implementation Complete** ✅

The plugin system is fully functional and ready for use. Developers can now create, deploy, and manage plugins to extend PicoPlus functionality.
