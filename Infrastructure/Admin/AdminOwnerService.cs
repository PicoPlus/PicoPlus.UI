using PicoPlus.Domain.Admin;
using PicoPlus.Application.Abstractions;
using PicoPlus.Services.CRM;

namespace PicoPlus.Infrastructure.Admin;

/// <summary>
/// Service for managing HubSpot owners in admin panel
/// </summary>
public class AdminOwnerService : IAdminOwnerService
{
    private readonly Owners _ownersService;
    private readonly ILogger<AdminOwnerService> _logger;

    public AdminOwnerService(
        Owners ownersService,
        ILogger<AdminOwnerService> logger)
    {
        _ownersService = ownersService;
        _logger = logger;
    }

    /// <summary>
    /// Get all HubSpot owners
    /// </summary>
    public async Task<List<HubSpotOwner>> GetAllOwnersAsync()
    {
        try
        {
            var response = await _ownersService.GetAll();
            
            if (response?.results == null)
            {
                _logger.LogWarning("No owners found in HubSpot");
                return new List<HubSpotOwner>();
            }

            var owners = response.results
                .Where(o => !o.archived)
                .Select(o => new HubSpotOwner
                {
                    Id = o.id,
                    Email = o.email,
                    FirstName = o.firstName,
                    LastName = o.lastName,
                    Archived = o.archived,
                    CreatedAt = o.createdAt,
                    UpdatedAt = o.updatedAt
                })
                .OrderBy(o => o.FullName)
                .ToList();

            _logger.LogInformation("Retrieved {Count} active owners from HubSpot", owners.Count);
            return owners;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving HubSpot owners");
            return new List<HubSpotOwner>();
        }
    }

    /// <summary>
    /// Search owners by email or name
    /// </summary>
    public async Task<List<HubSpotOwner>> SearchOwnersAsync(string searchTerm)
    {
        try
        {
            var allOwners = await GetAllOwnersAsync();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return allOwners;
            }

            var term = searchTerm.Trim().ToLower();
            
            return allOwners
                .Where(o => 
                    o.Email.ToLower().Contains(term) ||
                    o.FirstName.ToLower().Contains(term) ||
                    o.LastName.ToLower().Contains(term) ||
                    o.FullName.ToLower().Contains(term))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owners with term: {Term}", searchTerm);
            return new List<HubSpotOwner>();
        }
    }

    /// <summary>
    /// Get owner by ID
    /// </summary>
    public async Task<HubSpotOwner?> GetOwnerByIdAsync(string ownerId)
    {
        try
        {
            var allOwners = await GetAllOwnersAsync();
            return allOwners.FirstOrDefault(o => o.Id == ownerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner by ID: {OwnerId}", ownerId);
            return null;
        }
    }

    /// <summary>
    /// Get owner by email
    /// </summary>
    public async Task<HubSpotOwner?> GetOwnerByEmailAsync(string email)
    {
        try
        {
            var allOwners = await GetAllOwnersAsync();
            return allOwners.FirstOrDefault(o => 
                o.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner by email: {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Validate if owner exists
    /// </summary>
    public async Task<bool> ValidateOwnerAsync(string ownerIdOrEmail)
    {
        try
        {
            var allOwners = await GetAllOwnersAsync();
            
            return allOwners.Any(o => 
                o.Id == ownerIdOrEmail || 
                o.Email.Equals(ownerIdOrEmail, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating owner: {Owner}", ownerIdOrEmail);
            return false;
        }
    }
}
