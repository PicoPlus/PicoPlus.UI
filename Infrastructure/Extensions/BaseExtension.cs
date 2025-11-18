#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Base class for extensions providing default implementations.
/// </summary>
public abstract class BaseExtension : IExtension
{
    /// <inheritdoc />
    public abstract ExtensionMetadata Metadata { get; }

    /// <inheritdoc />
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Default: no services to register
    }

    /// <inheritdoc />
    public virtual void ConfigureApplication(WebApplication app)
    {
        // Default: no application configuration
    }
}
