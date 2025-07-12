using RestSharp;

namespace PicoPlus.Services.CRM;


public partial class Owners
{
    private readonly IConfiguration _configuration;
    public Owners(IConfiguration configuration) 
    { 
        _configuration = configuration;
    }
    public async  Task<Models.CRM.Owners.GetAll> GetAll()
    {
        var client = new RestClient("https://api.hubapi.com/crm/v3/owners/?limit=100&archived=false");
        var request = new RestRequest();
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", $"Bearer {_configuration["Hubspot:Token"]}");
        var response = await client.GetAsync<Models.CRM.Owners.GetAll>(request);
        return response;
    }
}
