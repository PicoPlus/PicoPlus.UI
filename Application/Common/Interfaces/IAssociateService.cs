#nullable enable

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for CRM object associations.
/// </summary>
public interface IAssociateService
{
    Task AssociateAsync(string fromObjectId, string fromObjectType,
                        string toObjectId,   string toObjectType);
    Task<IReadOnlyList<string>> GetAssociatedIdsAsync(string objectId, string objectType, string toObjectType);
}
