#nullable enable

using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Entities;

namespace NovinCRM.Services.CRM;

/// <summary>
/// Implements IOwnerRepository by delegating to the existing Owners service.
/// </summary>
public class OwnerRepository : IOwnerRepository
{
    private readonly Owners _owners;
    public OwnerRepository(Owners owners) => _owners = owners;

    public async Task<IReadOnlyList<Owner>> GetAllAsync()
    {
        var resp = await _owners.GetAll();
        return resp?.results?.Select(o => new Owner
        {
            Id        = o.id?.ToString() ?? string.Empty,
            Email     = o.email ?? string.Empty,
            FirstName = o.firstName,
            LastName  = o.lastName
        }).ToList() ?? (IReadOnlyList<Owner>)Array.Empty<Owner>();
    }

    public async Task<Owner?> FindByEmailAsync(string email)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(o => o.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
}
