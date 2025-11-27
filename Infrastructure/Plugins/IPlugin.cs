#nullable enable

namespace PicoPlus.Infrastructure.Plugins;

/// <summary>
/// Base interface for all plugins in the PicoPlus system.
/// Plugins must implement this interface to be discovered and loaded by the PluginManager.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the metadata information about this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Called when the plugin is loaded into the application.
    /// Use this to register services, initialize resources, etc.
    /// </summary>
    /// <param name="services">Service collection for dependency injection registration</param>
    /// <param name="context">Plugin context with application information</param>
    Task OnLoadAsync(IServiceCollection services, PluginContext context);

    /// <summary>
    /// Called when the plugin is unloaded from the application.
    /// Use this to clean up resources, dispose objects, etc.
    /// </summary>
    /// <param name="context">Plugin context with application information</param>
    Task OnUnloadAsync(PluginContext context);

    /// <summary>
    /// Called when the plugin is enabled.
    /// This is called after OnLoadAsync when the plugin is activated.
    /// </summary>
    /// <param name="context">Plugin context with application information</param>
    Task OnEnableAsync(PluginContext context);

    /// <summary>
    /// Called when the plugin is disabled.
    /// This is called before OnUnloadAsync when the plugin is deactivated.
    /// </summary>
    /// <param name="context">Plugin context with application information</param>
    Task OnDisableAsync(PluginContext context);
}
