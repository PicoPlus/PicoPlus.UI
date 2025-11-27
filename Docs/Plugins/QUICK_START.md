# Plugin System - Quick Start Guide

Welcome to the PicoPlus Plugin System! This guide will get you started quickly.

## What You Can Do

The plugin system allows you to:
- ✅ Extend PicoPlus functionality without modifying core code
- ✅ Load and unload plugins at runtime
- ✅ Manage plugins through a web UI
- ✅ Register custom services with dependency injection
- ✅ Access application configuration and logging

## Getting Started in 5 Minutes

### 1. View the Sample Plugin

The `SamplePlugin` directory contains a working example:

```bash
cd SamplePlugin
cat SamplePlugin.cs  # View the implementation
```

### 2. Build Your First Plugin

```bash
cd SamplePlugin
dotnet build -c Release
```

### 3. Deploy the Plugin

```bash
cp bin/Release/net9.0/SamplePlugin.dll ../Plugins/
```

### 4. Start the Application

When PicoPlus starts, it will automatically:
- Discover plugins in the `Plugins/` folder
- Load the plugin assemblies
- Register their services
- Enable plugins that were previously enabled

### 5. Manage Plugins

Navigate to:
```
http://localhost:5000/admin/plugins
```

Here you can:
- View all loaded plugins
- See plugin metadata (name, version, author, etc.)
- Enable/disable plugins
- Unload plugins from memory
- View plugin errors

## Creating Your Own Plugin

### Minimal Plugin Example

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
        Name = "My First Plugin",
        Version = "1.0.0",
        Description = "My awesome plugin"
    };

    public Task OnLoadAsync(IServiceCollection services, PluginContext context)
    {
        // Register your services here
        var logger = context.LoggerFactory.CreateLogger("MyPlugin");
        logger.LogInformation("Plugin loading!");
        return Task.CompletedTask;
    }

    public Task OnEnableAsync(PluginContext context)
    {
        // Called when plugin is enabled
        return Task.CompletedTask;
    }

    public Task OnDisableAsync(PluginContext context)
    {
        // Called when plugin is disabled
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(PluginContext context)
    {
        // Called when plugin is unloaded
        return Task.CompletedTask;
    }
}
```

### Project Setup

1. Create a new class library:
   ```bash
   dotnet new classlib -n MyPlugin -f net9.0
   ```

2. Add reference to PicoPlus in your `.csproj`:
   ```xml
   <ItemGroup>
     <ProjectReference Include="../PicoPlus.csproj">
       <Private>false</Private>
       <ExcludeAssets>runtime</ExcludeAssets>
     </ProjectReference>
   </ItemGroup>
   ```

3. Implement `IPlugin` interface

4. Build and copy DLL to `Plugins/` folder

5. Restart PicoPlus

## Plugin Capabilities

### Register Services

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    services.AddScoped<IMyService, MyService>();
    services.AddSingleton<IMyCache, MyCache>();
    return Task.CompletedTask;
}
```

### Access Configuration

```csharp
var apiKey = context.Configuration["MyPlugin:ApiKey"];
```

Add to `appsettings.json`:
```json
{
  "MyPlugin": {
    "ApiKey": "your-key"
  }
}
```

### Use Logging

```csharp
var logger = context.LoggerFactory.CreateLogger("MyPlugin");
logger.LogInformation("Important event");
logger.LogError(ex, "Something went wrong");
```

### Create Background Tasks

```csharp
private Timer? _timer;

public Task OnEnableAsync(PluginContext context)
{
    _timer = new Timer(
        callback: _ => DoWork(),
        state: null,
        dueTime: TimeSpan.Zero,
        period: TimeSpan.FromMinutes(5)
    );
    return Task.CompletedTask;
}

public Task OnDisableAsync(PluginContext context)
{
    _timer?.Dispose();
    return Task.CompletedTask;
}
```

## Directory Structure

```
PicoPlus.UI/
├── Plugins/                      # Put plugin DLLs here
│   ├── README.md
│   ├── plugin-state.json         # Auto-generated
│   └── YourPlugin.dll
├── SamplePlugin/                 # Example plugin
│   ├── SamplePlugin.cs
│   ├── SamplePlugin.csproj
│   └── README.md
├── Infrastructure/Plugins/       # Core plugin system
└── Docs/Plugins/                 # Documentation
    ├── README.md                 # Full documentation
    └── IMPLEMENTATION_GUIDE.md   # Detailed guide
```

## Common Tasks

### Enable a Plugin
1. Go to `/admin/plugins`
2. Click "Enable" on the plugin card
3. Plugin is now active

### Disable a Plugin
1. Go to `/admin/plugins`
2. Click "Disable" on the plugin card
3. Plugin remains loaded but inactive

### Unload a Plugin
1. Go to `/admin/plugins`
2. Click "Unload" on the plugin card
3. Plugin is removed from memory

### Debug a Plugin
1. Check application logs for errors
2. View errors in the plugin management UI
3. Enable detailed logging in `appsettings.json`

## Documentation

For more details, see:

- **[Complete Documentation](README.md)** - Full plugin system guide
- **[Implementation Guide](IMPLEMENTATION_GUIDE.md)** - Detailed reference
- **[Sample Plugin](../../SamplePlugin/README.md)** - Working example

## FAQ

**Q: Where do I put plugin DLLs?**  
A: In the `Plugins/` folder at the application root.

**Q: Do I need to restart the app after adding a plugin?**  
A: Yes, plugins are currently loaded at startup.

**Q: Can plugins access the database?**  
A: Yes, through services registered in the DI container.

**Q: Are plugins isolated from each other?**  
A: Yes, each plugin loads in its own `AssemblyLoadContext`.

**Q: Can I unload a plugin without restarting?**  
A: Yes, use the "Unload" button in the management UI.

**Q: How do I persist plugin data?**  
A: Use the application's configuration or database through DI services.

## Support

If you need help:
1. Check the documentation files
2. Review the sample plugin
3. Check application logs
4. File an issue on GitHub

## Next Steps

1. ✅ Read the [Complete Documentation](README.md)
2. ✅ Study the [Sample Plugin](../../SamplePlugin/)
3. ✅ Build your first plugin
4. ✅ Test in the management UI
5. ✅ Deploy to production

Happy plugin development! 🎉
