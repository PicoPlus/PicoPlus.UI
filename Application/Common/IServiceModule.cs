#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NovinCRM.Application.Common;

/// <summary>
/// A self-contained, feature-scoped DI registration module.
///
/// Implement this interface once per feature folder and place the implementation
/// class anywhere inside that folder.  <see cref="ServiceModuleExtensions.AddServiceModules"/>
/// discovers all implementations via assembly reflection and calls
/// <see cref="Register"/> for each one, so <c>Program.cs</c> never needs to be
/// edited when a new feature is added.
///
/// Example
/// -------
/// <code>
/// // Application/Features/Auth/AuthModule.cs
/// public sealed class AuthModule : IServiceModule
/// {
///     public void Register(IServiceCollection services, IConfiguration configuration)
///     {
///         services.AddScoped&lt;AuthService&gt;();
///         services.AddScoped&lt;OtpService&gt;();
///         services.AddScoped&lt;IRegisterService, RegisterService&gt;();
///     }
/// }
/// </code>
///
/// Closes issue #64 (MP-1).
/// </summary>
public interface IServiceModule
{
    /// <summary>Register services into <paramref name="services"/>.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">Application configuration (appsettings + env vars).</param>
    void Register(IServiceCollection services, IConfiguration configuration);
}

/// <summary>
/// Extension method that auto-discovers and registers all <see cref="IServiceModule"/>
/// implementations found in the calling assembly.
/// </summary>
public static class ServiceModuleExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblyMarker"/>'s assembly for all concrete
    /// <see cref="IServiceModule"/> implementations and calls
    /// <see cref="IServiceModule.Register"/> on each one.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="assemblyMarker">Any type in the assembly to scan (e.g. <c>typeof(Program)</c>).</param>
    public static IServiceCollection AddServiceModules(
        this IServiceCollection services,
        IConfiguration          configuration,
        Type                    assemblyMarker)
    {
        var moduleType = typeof(IServiceModule);

        var modules = assemblyMarker.Assembly
            .GetTypes()
            .Where(t => moduleType.IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract)
            .Select(t => (IServiceModule)Activator.CreateInstance(t)!)
            .ToList();

        foreach (var module in modules)
            module.Register(services, configuration);

        return services;
    }
}
