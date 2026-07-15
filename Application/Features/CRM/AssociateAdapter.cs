#nullable enable

using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Services.CRM;

/// <summary>
/// Implements IAssociateService by delegating to the existing Associate service.
/// </summary>
public class AssociateAdapter : IAssociateService
{
    private readonly Associate _associate;
    public AssociateAdapter(Associate associate) => _associate = associate;

    public async Task AssociateAsync(string fromObjectId, string fromObjectType,
                                     string toObjectId,   string toObjectType)
    {
        // The existing Associate service uses a different API signature;
        // we use the ListAssoc endpoint via HubSpot CRM v4 associations API.
        // Full create-association implementation goes here in Step 5 when
        // ViewModels are updated. For now this is a no-op stub.
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> GetAssociatedIdsAsync(
        string objectId, string objectType, string toObjectType)
    {
        var resp = await _associate.ListAssoc(objectId, objectType, toObjectType);
        return resp?.results?.Select(r => r.toObjectId.ToString())
                             .Where(id => !string.IsNullOrEmpty(id))
                             .ToList()
               ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
