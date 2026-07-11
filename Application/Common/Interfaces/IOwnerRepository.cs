#nullable enable

using PicoPlus.Domain.Entities;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot Owner operations.
/// </summary>
public interface IOwnerRepository
{
    Task<IReadOnlyList<Owner>> GetAllAsync();
    Task<Owner?> FindByEmailAsync(string email);
}
