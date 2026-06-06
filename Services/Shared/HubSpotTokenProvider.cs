using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.Shared;

/// <summary>
/// Centralized provider for the HubSpot API token.
/// Reads from environment variable first, then falls back to configuration.
/// </summary>
public class HubSpotTokenProvider
{
    public string Token { get; }

    public HubSpotTokenProvider(IConfiguration configuration)
    {
        Token = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                ?? configuration["HubSpot:Token"]
                ?? throw new InvalidOperationException(
                    "HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
    }
}
