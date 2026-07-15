#nullable enable

using NovinCRM.Domain.Entities;

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot Contact operations.
/// Implemented in Infrastructure. Application/ViewModels depend on this — never on the concrete class.
/// </summary>
public interface IContactRepository
{
    Task<Contact?> FindByNationalCodeAsync(string nationalCode);
    Task<Contact?> GetByIdAsync(string id);
    Task<Contact> CreateAsync(Contact contact);
    Task UpdateAsync(string id, Dictionary<string, string> properties);
    Task<bool> DeleteAsync(string id);
    Task<string?> UploadAvatarAsync(string contactId, byte[] imageBytes, string fileName = "avatar.jpg");

    /// <summary>
    /// Enriches a contact by fetching missing fields (father name, Shahkar status)
    /// from external identity services (Zohal). Returns the refreshed Domain entity.
    /// </summary>
    Task<Contact> UpdateMissingFieldsAsync(Contact contact, CancellationToken ct = default);
}
