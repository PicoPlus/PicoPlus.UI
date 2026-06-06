using RestSharp;
using Microsoft.Extensions.Configuration;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM
{
    public partial class Associate
    {
        private readonly IConfiguration _configuration;
        private readonly string _hubspotToken;

        public Associate(IConfiguration configuration, HubSpotTokenProvider tokenProvider)
        {
            _configuration = configuration;
            _hubspotToken = tokenProvider.Token;
        }

        public async Task<Models.CRM.Associate.ListAssoc.Response> ListAssoc(string ObjectID, string ObjectType, string ToObjectType)
        {
            var client = new RestClient($"https://api.hubapi.com/crm/v4/objects/{ObjectType}/{ObjectID}/associations/{ToObjectType}?limit=500");
            var request = new RestRequest();
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", $"Bearer {_hubspotToken}");

            var response = await client.GetAsync<Models.CRM.Associate.ListAssoc.Response>(request);

            return response;
        }
    }
}
