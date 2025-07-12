

namespace PicoPlus.Services.CRM
{

    public partial class Associate
    {

        public async Task<Models.CRM.Associate.ListAssoc.Response> ListAssoc(string ObjectID, string ObjectType, string ToObjectType)
        {
            var client = new RestClient($"https://api.hubapi.com/crm/v4/objects/{ObjectType}/{ObjectID}/associations/{ToObjectType}?limit=500");
            var request = new RestRequest();
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", "Bearer pat-eu1-d4ff038e-1ef6-46b9-a42b-8930513cf607");

            var response = await client.GetAsync<Models.CRM.Associate.ListAssoc.Response>(request);

            return response;
        }
    }
}
