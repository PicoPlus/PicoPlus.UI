# PicoPlus Plugin System

## Overview

The PicoPlus Plugin System provides a flexible, extensible architecture for adding functionality to your application through dynamically loaded plugins. Plugins are isolated assemblies that can be loaded, enabled, disabled, and unloaded at runtime.

## Key Features

- ✅ **Dynamic Loading**: Load plugins at runtime using `AssemblyLoadContext`
- ✅ **Isolation**: Each plugin loads in its own context for better isolation
- ✅ **Lifecycle Management**: Full control over plugin load, enable, disable, and unload
- ✅ **Service Registration**: Plugins can register their own services with DI
- ✅ **State Persistence**: Plugin enabled/disabled state is persisted across restarts
- ✅ **Management UI**: Built-in admin interface for managing plugins
- ✅ **Metadata Support**: Rich metadata including version, author, tags, dependencies

## Architecture

### Core Components

1. **IPlugin Interface**: The contract all plugins must implement
2. **PluginMetadata**: Descriptive information about a plugin
3. **PluginContext**: Runtime context passed to plugins during lifecycle events
4. **PluginManager**: Service that discovers, loads, and manages plugins
5. **PluginLoadContext**: Custom `AssemblyLoadContext` for isolated loading

### Plugin Lifecycle

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌──────────┐
│ Discover│ -> │  Load   │ -> │ Enable  │ -> │ Disable  │
└─────────┘    └─────────┘    └─────────┘    └──────────┘
                    │                              │
                    │                              │
                    └──────────> Unload <──────────┘
```

1. **Discovery**: `PluginManager` scans the `Plugins` folder for DLL files
2. **Load**: Plugin assembly is loaded via `AssemblyLoadContext`
   - `OnLoadAsync()` is called for service registration
3. **Enable**: Plugin is activated
   - `OnEnableAsync()` is called for initialization
4. **Disable**: Plugin is deactivated
   - `OnDisableAsync()` is called for cleanup
5. **Unload**: Plugin assembly is unloaded from memory
   - `OnUnloadAsync()` is called for final cleanup

## Getting Started

### Creating a Plugin

1. Create a new .NET class library project:

```bash
dotnet new classlib -n MyPlugin -f net9.0
```

2. Reference the PicoPlus project:

```xml
<ItemGroup>
  <ProjectReference Include="../PicoPlus.csproj">
    <Private>false</Private>
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
</ItemGroup>
```

3. Implement the `IPlugin` interface:

```csharp
using PicoPlus.Infrastructure.Plugins;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "my-plugin",
        Name = "My Plugin",
        Version = "1.0.0",
        Description = "Description of my plugin",
        Author = "Your Name",
        License = "MIT"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        // Register services with DI
        services.AddScoped<IMyService, MyService>();
        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context)
    {
        // Initialize plugin when enabled
        return Task.CompletedTask;
    }

    public Task OnDisableAsync(PluginContext context)
    {
        // Cleanup when disabled
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(PluginContext context)
    {
        // Final cleanup before unload
        return Task.CompletedTask;
    }
}
```

4. Build the plugin:

```bash
dotnet build MyPlugin.csproj -c Release
```

5. Copy the DLL to the `Plugins` folder:

```bash
cp bin/Release/net9.0/MyPlugin.dll ../Plugins/
```

### Deploying a Plugin

1. Copy your plugin DLL to the `Plugins` folder in the application root
2. Restart the application (plugins are loaded at startup)
3. Navigate to `/admin/plugins` to manage the plugin
4. Enable the plugin through the UI

## Plugin Metadata

The `PluginMetadata` class provides information about your plugin:

```csharp
public PluginMetadata Metadata => new()
{
    // Required fields
    Id = "unique-plugin-id",          // Unique identifier
    Name = "Plugin Display Name",      // User-friendly name
    Version = "1.0.0",                // Semantic version
    Description = "What this plugin does",
    
    // Optional fields
    Author = "Your Name",              // Plugin author
    Website = "https://example.com",   // Plugin website
    License = "MIT",                   // License type
    Tags = new[] { "tag1", "tag2" },  // Searchable tags
    MinimumPicoPlusVersion = "1.0.0", // Compatibility info
    Dependencies = new[] { "other-plugin" } // Plugin dependencies
};
```

## Plugin Context

The `PluginContext` provides access to application services:

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    // Access configuration
    var setting = context.Configuration["MySetting"];
    
    // Create a logger
    var logger = context.LoggerFactory.CreateLogger<MyPlugin>();
    logger.LogInformation("Plugin loading...");
    
    // Access environment info
    var isProduction = context.Environment.IsProduction();
    
    // Get plugin directory
    var pluginDir = context.PluginPath;
    
    // Store custom data
    context.Data["MyKey"] = myValue;
    
    return Task.CompletedTask;
}
```

## Service Registration

Plugins can register services with the application's DI container:

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    // Register scoped services
    services.AddScoped<IMyService, MyService>();
    
    // Register singleton services
    services.AddSingleton<IMyCache, MyCache>();
    
    // Register with factory
    services.AddScoped<IMyFactory>(sp => 
        new MyFactory(sp.GetRequiredService<ILogger<MyFactory>>()));
    
    return Task.CompletedTask;
}
```

## Best Practices

### Do's ✅

- ✅ Use semantic versioning for your plugin
- ✅ Provide clear, descriptive metadata
- ✅ Log important events and errors
- ✅ Clean up resources in `OnDisableAsync` and `OnUnloadAsync`
- ✅ Handle exceptions gracefully
- ✅ Use dependency injection for services
- ✅ Document your plugin's features and requirements
- ✅ Test your plugin thoroughly before deployment

### Don'ts ❌

- ❌ Don't modify shared application state unsafely
- ❌ Don't hold onto unmanaged resources without cleanup
- ❌ Don't block lifecycle methods with long-running operations
- ❌ Don't use static state that can't be cleaned up
- ❌ Don't assume plugin load order
- ❌ Don't bundle the entire PicoPlus assembly with your plugin

## Example: Adding a Menu Item

Here's a complete example of a plugin that adds a menu action:

```csharp
using PicoPlus.Infrastructure.Plugins;

namespace MenuPlugin;

public class MenuPlugin : IPlugin
{
    public PluginMetadata Metadata => new()
    {
        Id = "menu-plugin",
        Name = "Custom Menu Plugin",
        Version = "1.0.0",
        Description = "Adds a custom menu item to the application"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        // Register a service that provides menu items
        services.AddScoped<IMenuProvider, CustomMenuProvider>();
        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context)
    {
        var logger = context.LoggerFactory.CreateLogger<MenuPlugin>();
        logger.LogInformation("Custom menu items are now available");
        return Task.CompletedTask;
    }

    public Task OnDisableAsync(PluginContext context)
    {
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(PluginContext context)
    {
        return Task.CompletedTask;
    }
}

public interface IMenuProvider
{
    List<MenuItem> GetMenuItems();
}

public class CustomMenuProvider : IMenuProvider
{
    public List<MenuItem> GetMenuItems()
    {
        return new List<MenuItem>
        {
            new MenuItem { Title = "Custom Action", Url = "/custom/action" }
        };
    }
}

public class MenuItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
```

## Troubleshooting

### Plugin Not Loading

1. Check that the DLL is in the `Plugins` folder
2. Verify the plugin implements `IPlugin`
3. Check application logs for errors
4. Ensure the plugin targets .NET 9.0

### Plugin Enabled But Not Working

1. Check that services are registered correctly
2. Verify `OnEnableAsync` completed without errors
3. Check for dependency conflicts
4. Review plugin logs

### Can't Unload Plugin

1. Ensure `OnDisableAsync` is called first
2. Check for held references preventing GC
3. Verify no background tasks are still running
4. Review error messages in the UI

## Management UI

The plugin management interface is available at `/admin/plugins`:

- **View**: See all loaded plugins with their metadata
- **Enable**: Activate a disabled plugin
- **Disable**: Deactivate an enabled plugin
- **Unload**: Remove a plugin from memory

## Security Considerations

- Only load plugins from trusted sources
- Plugins run with the same permissions as the main application
- Review plugin source code before deployment
- Monitor plugin behavior in production
- Keep plugins updated for security patches

## Advanced Topics

### Plugin Dependencies

Specify dependencies in metadata:

```csharp
Dependencies = new[] { "required-plugin-id" }
```

The PluginManager doesn't enforce dependencies currently - this is metadata only.

### Configuration

Plugins can access application configuration:

```csharp
var apiKey = context.Configuration["MyPlugin:ApiKey"];
```

Add plugin settings to `appsettings.json`:

```json
{
  "MyPlugin": {
    "ApiKey": "your-key-here"
  }
}
```

### State Persistence

Plugin enabled/disabled state is automatically persisted in `Plugins/plugin-state.json`.

## API Reference

See the inline documentation in:
- `Infrastructure/Plugins/IPlugin.cs`
- `Infrastructure/Plugins/PluginMetadata.cs`
- `Infrastructure/Plugins/PluginContext.cs`
- `Infrastructure/Plugins/PluginManager.cs`

## Support

For questions or issues:
1. Check this documentation
2. Review the sample plugin
3. Check application logs
4. File an issue on GitHub
