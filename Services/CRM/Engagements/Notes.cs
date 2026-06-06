using RestSharp;

namespace PicoPlus.Services.CRM.Engagements
{
    public partial class Notes
    {
        private readonly IConfiguration _configuration;

        public Notes(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Models.CRM.Engagements.Notes.ReadSingle> GetSingleNote(string noteid)
        {
            var client = new RestClient($"https://api.hubapi.com/crm/v3/objects/notes/{noteid}?archived=false");
            var request = new RestRequest();
            request.AddHeader("accept", "application/json");

            var token = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? _configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable.");
            request.AddHeader("authorization", $"Bearer {token}");
            request.AddQueryParameter("properties", "hs_attachment_ids");
            request.AddQueryParameter("properties", "hs_note_body");

            var response = await client.GetAsync<Models.CRM.Engagements.Notes.ReadSingle>(request);
            return response;
        }
        public async Task CreateNote()
        {

        }
    }
}
