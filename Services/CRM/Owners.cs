using RestSharp;
using Microsoft.Extensions.Configuration;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM;

public partial class Owners
{
    private readonly IConfiguration _configuration;
    private readonly string _hubspotToken;

    public Owners(IConfiguration configuration, HubSpotTokenProvider tokenProvider)
    {
        _configuration = configuration;
        _hubspotToken = tokenProvider.Token;
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
