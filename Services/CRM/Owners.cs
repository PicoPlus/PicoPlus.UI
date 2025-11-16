using RestSharp;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM;

public partial class Owners
{
    private readonly IConfiguration _configuration;
    private readonly string _hubspotToken;

    public Owners(IConfiguration configuration)
    {
        _configuration = configuration;

        // Read from environment variable first, then configuration
        _hubspotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
    }

    public async Task<Models.CRM.Owners.GetAll> GetAll()
    {
        var client = new RestClient("https://api.hubapi.com/crm/v3/owners/?limit=100&archived=false");
        var request = new RestRequest();
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", $"Bearer {_hubspotToken}");
        var response = await client.GetAsync<Models.CRM.Owners.GetAll>(request);
        return response;
    }
}
