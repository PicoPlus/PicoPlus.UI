using RestSharp;

namespace PicoPlus.Services.CRM.Engagements
{
    public partial class Notes
    {
        public async Task<Models.CRM.Engagements.Notes.ReadSingle> GetSingleNote(string noteid)
        {
            var client = new RestClient($"https://api.hubapi.com/crm/v3/objects/notes/{noteid}?archived=false");
            var request = new RestRequest();
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", "Bearer pat-eu1-d4ff038e-1ef6-46b9-a42b-8930513cf607");
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
