#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Manages discovery, loading, and lifecycle of extensions.
/// </summary>
public class ExtensionManager
{
    private readonly List<IExtension> _extensions = new();
    private readonly ILogger<ExtensionManager> _logger;
    private readonly ExtensionOptions _options;

    public ExtensionManager(ILogger<ExtensionManager> logger, ExtensionOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new ExtensionOptions();
    }

    /// <summary>
    /// Gets all loaded extensions.
    /// </summary>
    public IReadOnlyList<IExtension> Extensions => _extensions.AsReadOnly();

    /// <summary>
    /// Discovers and loads extensions from the current assembly.
    /// </summary>
    public void DiscoverExtensions()
    {
        _logger.LogInformation("Starting extension discovery...");

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var extensionTypes = assembly.GetTypes()
                .Where(t => typeof(IExtension).IsAssignableFrom(t) 
                         && !t.IsInterface 
                         && !t.IsAbstract)
                .ToList();

            _logger.LogInformation("Found {Count} extension type(s)", extensionTypes.Count);

            foreach (var type in extensionTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is IExtension extension)
                    {
                        var isEnabled = IsExtensionEnabled(extension);
                        
                        if (isEnabled)
                        {
                            _extensions.Add(extension);
                            _logger.LogInformation("Loaded extension: {Name} (v{Version})", 
                                extension.Metadata.Name, 
                                extension.Metadata.Version);
                        }
                        else
                        {
                            _logger.LogInformation("Skipped disabled extension: {Name}", 
                                extension.Metadata.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load extension type: {TypeName}", type.Name);
                }
            }

            // Sort extensions by dependencies
            _extensions.Sort(new ExtensionDependencyComparer());

            _logger.LogInformation("Extension discovery completed. Loaded {Count} extension(s)", 
                _extensions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during extension discovery");
        }
    }

    /// <summary>
    /// Configures services for all loaded extensions.
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        foreach (var extension in _extensions)
        {
            try
            {
                _logger.LogDebug("Configuring services for extension: {Name}", 
                    extension.Metadata.Name);
                extension.ConfigureServices(services, configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure services for extension: {Name}", 
                    extension.Metadata.Name);
            }
        }
    }

    /// <summary>
    /// Configures application pipeline for all loaded extensions.
    /// </summary>
    public void ConfigureApplication(WebApplication app)
    {
        foreach (var extension in _extensions)
        {
            try
            {
                _logger.LogDebug("Configuring application for extension: {Name}", 
                    extension.Metadata.Name);
                extension.ConfigureApplication(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure application for extension: {Name}", 
                    extension.Metadata.Name);
            }
        }
    }

    private bool IsExtensionEnabled(IExtension extension)
    {
        var extensionId = extension.Metadata.Id;

        // Check if explicitly disabled
        if (_options.DisabledExtensions.Contains(extensionId))
        {
            return false;
        }

        // Check if explicitly enabled
        if (_options.EnabledExtensions.Contains(extensionId))
        {
            return true;
        }

        // Use default setting
        return extension.Metadata.EnabledByDefault;
    }
}

/// <summary>
/// Comparer to sort extensions by their dependencies.
/// </summary>
internal class ExtensionDependencyComparer : IComparer<IExtension>
{
    public int Compare(IExtension? x, IExtension? y)
    {
        if (x == null || y == null) return 0;

        // If x depends on y, x should come after y
        if (x.Metadata.Dependencies.Contains(y.Metadata.Id))
        {
            return 1;
        }

        // If y depends on x, y should come after x
        if (y.Metadata.Dependencies.Contains(x.Metadata.Id))
        {
            return -1;
        }

        // No dependency relationship
        return string.Compare(x.Metadata.Id, y.Metadata.Id, StringComparison.Ordinal);
    }
}
