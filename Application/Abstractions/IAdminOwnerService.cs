using PicoPlus.Domain.Admin;

namespace PicoPlus.Application.Abstractions;

public interface IAdminOwnerService
{
    Task<List<HubSpotOwner>> GetAllOwnersAsync();
    Task<List<HubSpotOwner>> SearchOwnersAsync(string searchTerm);
    Task<HubSpotOwner?> GetOwnerByIdAsync(string ownerId);
    Task<HubSpotOwner?> GetOwnerByEmailAsync(string email);
    Task<bool> ValidateOwnerAsync(string ownerIdOrEmail);
}
