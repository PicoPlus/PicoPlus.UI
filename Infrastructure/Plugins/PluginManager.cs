#nullable enable

using System.Reflection;
using System.Text.Json;

namespace PicoPlus.Infrastructure.Plugins;

/// <summary>
/// Manages the discovery, loading, and lifecycle of plugins.
/// </summary>
public class PluginManager
{
    private readonly IServiceCollection _services;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PluginManager> _logger;
    private readonly string _pluginsDirectory;
    private readonly Dictionary<string, PluginInfo> _loadedPlugins = new();
    private readonly string _pluginStateFile;

    public PluginManager(
        IServiceCollection services,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        _services = services;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PluginManager>();

        // Determine plugins directory with configurable path support
        // Priority: 1) PICOPLUS_PLUGINS_PATH env var, 2) /tmp/Plugins, 3) ContentRootPath/Plugins
        var envPluginsPath = Environment.GetEnvironmentVariable("PICOPLUS_PLUGINS_PATH");
        
        if (!string.IsNullOrWhiteSpace(envPluginsPath))
        {
            _pluginsDirectory = envPluginsPath;
            _logger.LogInformation("Using plugins directory from PICOPLUS_PLUGINS_PATH: {PluginsDirectory}", _pluginsDirectory);
        }
        else if (Directory.Exists("/tmp") || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            _pluginsDirectory = "/tmp/Plugins";
            _logger.LogInformation("Using default writable plugins directory: {PluginsDirectory}", _pluginsDirectory);
        }
        else
        {
            _pluginsDirectory = Path.Combine(_environment.ContentRootPath, "Plugins");
            _logger.LogInformation("Using application root plugins directory: {PluginsDirectory}", _pluginsDirectory);
        }

        _pluginStateFile = Path.Combine(_pluginsDirectory, "plugin-state.json");

        // Ensure plugins directory exists
        try
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
                _logger.LogInformation("Created plugins directory: {PluginsDirectory}", _pluginsDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create plugins directory: {PluginsDirectory}. The application may not function correctly.", _pluginsDirectory);
        }
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyDictionary<string, PluginInfo> LoadedPlugins => _loadedPlugins;

    /// <summary>
    /// Discovers and loads all plugins from the plugins directory.
    /// </summary>
    public async Task DiscoverAndLoadPluginsAsync()
    {
        _logger.LogInformation("Discovering plugins in: {PluginsDirectory}", _pluginsDirectory);

        // Load plugin state (enabled/disabled)
        var pluginStates = LoadPluginStates();

        // Find all DLL files in the plugins directory
        var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        
        _logger.LogInformation("Found {Count} plugin DLL files", pluginFiles.Length);

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                await LoadPluginAsync(pluginFile, pluginStates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from: {PluginFile}", pluginFile);
            }
        }

        _logger.LogInformation("Loaded {Count} plugins", _loadedPlugins.Count);
    }

    /// <summary>
    /// Loads a single plugin from the specified assembly file.
    /// </summary>
    private async Task LoadPluginAsync(string assemblyPath, Dictionary<string, bool> pluginStates)
    {
        _logger.LogDebug("Loading plugin from: {AssemblyPath}", assemblyPath);

        // Create a custom load context for this plugin
        var loadContext = new PluginLoadContext(assemblyPath);
        
        try
        {
            // Load the plugin assembly
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find types that implement IPlugin
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            if (pluginTypes.Count == 0)
            {
                _logger.LogWarning("No IPlugin implementations found in: {AssemblyPath}", assemblyPath);
                loadContext.Unload();
                return;
            }

            if (pluginTypes.Count > 1)
            {
                _logger.LogWarning("Multiple IPlugin implementations found in {AssemblyPath}, using first one", assemblyPath);
            }

            // Create an instance of the plugin
            var pluginType = pluginTypes[0];
            var plugin = Activator.CreateInstance(pluginType) as IPlugin;

            if (plugin == null)
            {
                _logger.LogError("Failed to create instance of plugin type: {PluginType}", pluginType.FullName);
                loadContext.Unload();
                return;
            }

            // Check if plugin should be enabled
            var shouldEnable = !pluginStates.ContainsKey(plugin.Metadata.Id) || pluginStates[plugin.Metadata.Id];

            // Create plugin info
            var pluginInfo = new PluginInfo
            {
                Plugin = plugin,
                AssemblyPath = assemblyPath,
                Assembly = assembly,
                LoadContext = loadContext,
                IsEnabled = false // Will be set to true if OnEnableAsync succeeds
            };

            // Create plugin context
            var context = new PluginContext
            {
                ServiceProvider = _serviceProvider,
                Configuration = _configuration,
                Environment = _environment,
                LoggerFactory = _loggerFactory,
                PluginPath = Path.GetDirectoryName(assemblyPath) ?? _pluginsDirectory
            };

            // Call OnLoadAsync
            await plugin.OnLoadAsync(_services, context);
            _logger.LogInformation("Plugin loaded: {PluginName} v{Version}", 
                plugin.Metadata.Name, plugin.Metadata.Version);

            // Call OnEnableAsync if the plugin should be enabled
            if (shouldEnable)
            {
                await plugin.OnEnableAsync(context);
                pluginInfo.IsEnabled = true;
                _logger.LogInformation("Plugin enabled: {PluginName}", plugin.Metadata.Name);
            }

            // Store the plugin info
            _loadedPlugins[plugin.Metadata.Id] = pluginInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from: {AssemblyPath}", assemblyPath);
            loadContext.Unload();
            throw;
        }
    }

    /// <summary>
    /// Enables a loaded plugin.
    /// </summary>
    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.LogWarning("Plugin not found: {PluginId}", pluginId);
            return false;
        }

        if (pluginInfo.IsEnabled)
        {
            _logger.LogWarning("Plugin already enabled: {PluginId}", pluginId);
            return true;
        }

        try
        {
            var context = new PluginContext
            {
                ServiceProvider = _serviceProvider,
                Configuration = _configuration,
                Environment = _environment,
                LoggerFactory = _loggerFactory,
                PluginPath = Path.GetDirectoryName(pluginInfo.AssemblyPath) ?? _pluginsDirectory
            };

            await pluginInfo.Plugin.OnEnableAsync(context);
            pluginInfo.IsEnabled = true;
            
            // Save state
            SavePluginState(pluginId, true);
            
            _logger.LogInformation("Plugin enabled: {PluginName}", pluginInfo.Plugin.Metadata.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable plugin: {PluginId}", pluginId);
            pluginInfo.Errors.Add($"Enable failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disables a loaded plugin.
    /// </summary>
    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.LogWarning("Plugin not found: {PluginId}", pluginId);
            return false;
        }

        if (!pluginInfo.IsEnabled)
        {
            _logger.LogWarning("Plugin already disabled: {PluginId}", pluginId);
            return true;
        }

        try
        {
            var context = new PluginContext
            {
                ServiceProvider = _serviceProvider,
                Configuration = _configuration,
                Environment = _environment,
                LoggerFactory = _loggerFactory,
                PluginPath = Path.GetDirectoryName(pluginInfo.AssemblyPath) ?? _pluginsDirectory
            };

            await pluginInfo.Plugin.OnDisableAsync(context);
            pluginInfo.IsEnabled = false;
            
            // Save state
            SavePluginState(pluginId, false);
            
            _logger.LogInformation("Plugin disabled: {PluginName}", pluginInfo.Plugin.Metadata.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable plugin: {PluginId}", pluginId);
            pluginInfo.Errors.Add($"Disable failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unloads a plugin from memory.
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.LogWarning("Plugin not found: {PluginId}", pluginId);
            return false;
        }

        try
        {
            // Disable first if enabled
            if (pluginInfo.IsEnabled)
            {
                await DisablePluginAsync(pluginId);
            }

            var context = new PluginContext
            {
                ServiceProvider = _serviceProvider,
                Configuration = _configuration,
                Environment = _environment,
                LoggerFactory = _loggerFactory,
                PluginPath = Path.GetDirectoryName(pluginInfo.AssemblyPath) ?? _pluginsDirectory
            };

            // Call OnUnloadAsync
            await pluginInfo.Plugin.OnUnloadAsync(context);

            // Unload the assembly
            pluginInfo.LoadContext.Unload();

            // Remove from loaded plugins
            _loadedPlugins.Remove(pluginId);

            _logger.LogInformation("Plugin unloaded: {PluginName}", pluginInfo.Plugin.Metadata.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin: {PluginId}", pluginId);
            pluginInfo.Errors.Add($"Unload failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a plugin by its ID.
    /// </summary>
    public PluginInfo? GetPlugin(string pluginId)
    {
        _loadedPlugins.TryGetValue(pluginId, out var pluginInfo);
        return pluginInfo;
    }

    /// <summary>
    /// Loads plugin states from the state file.
    /// </summary>
    private Dictionary<string, bool> LoadPluginStates()
    {
        if (!File.Exists(_pluginStateFile))
        {
            return new Dictionary<string, bool>();
        }

        try
        {
            var json = File.ReadAllText(_pluginStateFile);
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) 
                ?? new Dictionary<string, bool>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin states from: {StateFile}", _pluginStateFile);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Saves the enabled state of a plugin.
    /// </summary>
    private void SavePluginState(string pluginId, bool enabled)
    {
        try
        {
            var states = LoadPluginStates();
            states[pluginId] = enabled;

            var json = JsonSerializer.Serialize(states, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_pluginStateFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save plugin state for: {PluginId}", pluginId);
        }
    }
}
