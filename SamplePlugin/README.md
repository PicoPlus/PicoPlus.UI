# Sample Plugin for PicoPlus

This is a sample plugin demonstrating the PicoPlus plugin architecture. Use it as a template for creating your own plugins.

## What This Plugin Demonstrates

- ✅ Implementing the `IPlugin` interface
- ✅ Providing plugin metadata
- ✅ Registering services with dependency injection
- ✅ Handling lifecycle events (Load, Enable, Disable, Unload)
- ✅ Logging plugin activities

## Project Structure

```
SamplePlugin/
├── SamplePlugin.csproj    # Project file with PicoPlus reference
├── SamplePlugin.cs        # Plugin implementation
└── README.md             # This file
```

## Building the Plugin

1. Build the project:

```bash
dotnet build SamplePlugin.csproj -c Release
```

2. The compiled DLL will be in `bin/Release/net9.0/SamplePlugin.dll`

## Deploying the Plugin

1. Copy the DLL to the Plugins folder:

```bash
cp bin/Release/net9.0/SamplePlugin.dll ../Plugins/
```

2. Restart the PicoPlus application

3. Navigate to `/admin/plugins` to see and manage the plugin

## Plugin Features

### Metadata

- **ID**: `sample-plugin`
- **Name**: Sample Plugin
- **Version**: 1.0.0
- **Description**: A sample plugin demonstrating the PicoPlus plugin architecture

### Services

The plugin registers:
- `ISampleService` - An example service interface
- `SampleService` - The implementation that returns a greeting message

### Using the Plugin Service

Once the plugin is enabled, you can inject `ISampleService` in your components:

```csharp
@inject ISampleService SampleService

<p>@SampleService.GetMessage()</p>
```

## Customizing This Plugin

To create your own plugin based on this sample:

1. **Change the metadata**:
   - Update `Id`, `Name`, `Version`, `Description`
   - Add your name as `Author`
   - Update `Tags` to describe your plugin

2. **Add your services**:
   - Define your service interfaces
   - Implement your service classes
   - Register them in `OnLoadAsync`

3. **Implement initialization logic**:
   - Add startup code in `OnEnableAsync`
   - Add cleanup code in `OnDisableAsync`

4. **Add resources**:
   - Database connections
   - Configuration files
   - API clients
   - Background tasks

## Example: Adding a Background Task

```csharp
private Timer? _timer;

public Task OnEnableAsync(PluginContext context)
{
    var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
    logger.LogInformation("Starting background task...");
    
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

private void DoWork(PluginContext context)
{
    var logger = context.LoggerFactory.CreateLogger<SamplePlugin>();
    logger.LogInformation("Background task executing...");
    // Do your work here
}
```

## Example: Reading Configuration

```csharp
public Task OnLoadAsync(IServiceCollection services, PluginContext context)
{
    var apiKey = context.Configuration["SamplePlugin:ApiKey"];
    var timeout = context.Configuration.GetValue<int>("SamplePlugin:Timeout", 30);
    
    services.AddScoped<ISampleService>(sp => 
        new SampleService(apiKey, timeout));
    
    return Task.CompletedTask;
}
```

Add to `appsettings.json`:

```json
{
  "SamplePlugin": {
    "ApiKey": "your-key-here",
    "Timeout": 60
  }
}
```

## Testing Your Plugin

1. Build and deploy the plugin
2. Check application logs for loading messages
3. Visit `/admin/plugins` to verify it's loaded
4. Enable the plugin
5. Test the plugin's functionality
6. Check for any errors in the UI or logs

## Troubleshooting

**Plugin doesn't appear in the list:**
- Check that the DLL is in the `Plugins` folder
- Verify the project references PicoPlus correctly
- Check application logs for loading errors

**Plugin shows errors:**
- Review the error messages in the plugin management UI
- Check application logs for detailed stack traces
- Verify all dependencies are available

**Services not working:**
- Ensure the plugin is enabled (not just loaded)
- Verify services are registered correctly in `OnLoadAsync`
- Check that you're using the correct interface name

## Resources

- [Main Plugin Documentation](../Docs/Plugins/README.md)
- [PicoPlus Plugin API Reference](../Infrastructure/Plugins/)

## License

MIT License - Feel free to use this as a template for your own plugins.
