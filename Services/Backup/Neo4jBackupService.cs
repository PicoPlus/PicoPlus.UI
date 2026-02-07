using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using System.Text.Json;
using PicoPlus.State.UserPanel;

namespace PicoPlus.Services.Backup;

/// <summary>
/// Best-effort Neo4j backup writer. It never throws to the caller.
/// Enable by setting NEO4J_URI, NEO4J_USER, NEO4J_PASSWORD environment variables.
/// </summary>
public class Neo4jBackupService : IGraphBackupService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Neo4jBackupService> _logger;

    public Neo4jBackupService(IHttpClientFactory httpClientFactory, ILogger<Neo4jBackupService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task BackupUserPanelStateAsync(string contactId, UserPanelState state, CancellationToken cancellationToken = default)
    {
        var uri = Environment.GetEnvironmentVariable("NEO4J_URI");
        var user = Environment.GetEnvironmentVariable("NEO4J_USER");
        var password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD");

        if (string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogDebug("Neo4j backup skipped: missing configuration.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var endpoint = uri.TrimEnd('/') + "/db/neo4j/tx/commit";
            var cypher = @"
MERGE (c:Contact {id: $contactId})
SET c.firstName = $firstName,
    c.lastName = $lastName,
    c.phone = $phone,
    c.wallet = $wallet,
    c.syncedAt = datetime()
WITH c
UNWIND $deals AS d
MERGE (deal:Deal {id: d.id})
SET deal.name = d.name,
    deal.amount = d.amount,
    deal.stage = d.stage,
    deal.updatedAt = d.updatedAt
MERGE (c)-[:HAS_DEAL]->(deal);
";

            var payload = new
            {
                statements = new[]
                {
                    new
                    {
                        statement = cypher,
                        parameters = new
                        {
                            contactId,
                            firstName = state.Contact.FirstName,
                            lastName = state.Contact.LastName,
                            phone = state.Contact.Phone,
                            wallet = state.Contact.Wallet,
                            deals = state.Deals.Select(d => new
                            {
                                id = d.Id,
                                name = d.DealName,
                                amount = d.Amount,
                                stage = d.Stage.ToString(),
                                updatedAt = d.UpdatedAt
                            }).ToArray()
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Neo4j backup failed for contact {ContactId} with status {StatusCode}", contactId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j backup failed for contact {ContactId}", contactId);
        }
    }
}
