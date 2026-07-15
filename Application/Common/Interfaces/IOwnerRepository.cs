#nullable enable

using NovinCRM.Domain.Entities;

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot Owner operations.
/// </summary>
public interface IOwnerRepository
{
    Task<IReadOnlyList<Owner>> GetAllAsync();
    Task<Owner?> FindByEmailAsync(string email);
}
