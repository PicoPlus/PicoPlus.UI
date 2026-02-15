using PicoPlus.Models.CRM;
using PicoPlus.Models.CRM.Commerce;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.Infrastructure;

public interface IHubSpotClient
{
    Task<Associate.ListAssoc.Response?> ListAssociationsAsync(string objectId, string objectType, string toObjectType, CancellationToken cancellationToken = default);
    Task<Deal.GetBatch.Response?> GetDealsBatchAsync(IEnumerable<string> dealIds, CancellationToken cancellationToken = default);
    Task<LineItem.Read.Response?> GetLineItemAsync(string lineItemId, CancellationToken cancellationToken = default);
    Task<Contact.Read.Response?> ReadContactAsync(string contactId, IEnumerable<string> properties, CancellationToken cancellationToken = default);
    Task UpdateContactPropertiesAsync(string contactId, Dictionary<string, string> properties, CancellationToken cancellationToken = default);
}
