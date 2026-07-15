using RestSharp;
using System.Text.Json;

namespace NovinCRM.Services.CRM.Engagements
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

        /// <summary>
        /// Creates a HubSpot Note and associates it with a deal and/or contact.
        /// Returns the new note's HubSpot ID, or null on failure.
        /// </summary>
        public async Task<string?> CreateNoteAsync(
            string  body,
            string? associatedDealId    = null,
            string? associatedContactId = null)
        {
            try
            {
                var token   = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                              ?? "pat-eu1-d4ff038e-1ef6-46b9-a42b-8930513cf607";
                var client  = new RestClient("https://api.hubapi.com");
                var request = new RestRequest("/crm/v3/objects/notes", Method.Post);
                request.AddHeader("authorization", $"Bearer {token}");
                request.AddHeader("content-type", "application/json");

                var associations = new List<object>();

                if (!string.IsNullOrEmpty(associatedDealId))
                {
                    associations.Add(new
                    {
                        to    = new { id = associatedDealId },
                        types = new[] { new { associationCategory = "HUBSPOT_DEFINED", associationTypeId = 214 } }
                    });
                }

                if (!string.IsNullOrEmpty(associatedContactId))
                {
                    associations.Add(new
                    {
                        to    = new { id = associatedContactId },
                        types = new[] { new { associationCategory = "HUBSPOT_DEFINED", associationTypeId = 202 } }
                    });
                }

                var payload = new
                {
                    properties = new
                    {
                        hs_note_body = body,
                        hs_timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    },
                    associations = associations.Count > 0 ? associations : null
                };

                request.AddJsonBody(JsonSerializer.Serialize(payload));

                var resp = await client.ExecuteAsync(request);
                if (!resp.IsSuccessful || resp.Content == null) return null;

                using var doc = JsonDocument.Parse(resp.Content);
                return doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Legacy empty stub — kept for binary compatibility.</summary>
        [Obsolete("Use CreateNoteAsync instead.")]
        public async Task CreateNote() { await Task.CompletedTask; }
    }
}
