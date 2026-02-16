using Microsoft.Extensions.Caching.Memory;
using PicoPlus.Models.Admin;
using PicoPlus.Services.CRM;

namespace PicoPlus.Services.Admin;

/// <summary>
/// Service for managing HubSpot owners in admin panel
/// </summary>
public class AdminOwnerService
{
    private const string OwnersCacheKey = "admin:hubspot:owners:all";
    private static readonly TimeSpan OwnersCacheTtl = TimeSpan.FromMinutes(10);

    private readonly Owners _ownersService;
    private readonly ILogger<AdminOwnerService> _logger;
    private readonly IMemoryCache _cache;

    public AdminOwnerService(
        Owners ownersService,
        ILogger<AdminOwnerService> logger,
        IMemoryCache cache)
    {
        _ownersService = ownersService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Get all HubSpot owners
    /// </summary>
    public async Task<List<HubSpotOwner>> GetAllOwnersAsync()
    {
        try
        {
            if (_cache.TryGetValue<List<HubSpotOwner>>(OwnersCacheKey, out var cachedOwners) && cachedOwners is not null)
            {
                _logger.LogDebug("Owners cache hit. Count: {Count}", cachedOwners.Count);
                return cachedOwners;
            }

            _logger.LogDebug("Owners cache miss. Fetching owners from HubSpot.");
            var owners = await FetchOwnersFromUpstreamAsync();

            _cache.Set(OwnersCacheKey, owners, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = OwnersCacheTtl,
                Size = 1
            });

            return owners;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving HubSpot owners");
            return new List<HubSpotOwner>();
        }
    }

    /// <summary>
    /// Explicitly invalidate owners cache.
    /// Call this after owner create/update operations.
    /// </summary>
    public void InvalidateOwnerCache()
    {
        _cache.Remove(OwnersCacheKey);
        _logger.LogInformation("Owners cache invalidated.");
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

            var term = searchTerm.Trim();

            return allOwners
                .Where(o =>
                    ContainsInsensitive(o.Email, term) ||
                    ContainsInsensitive(o.FirstName, term) ||
                    ContainsInsensitive(o.LastName, term) ||
                    ContainsInsensitive(o.FullName, term))
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

    private async Task<List<HubSpotOwner>> FetchOwnersFromUpstreamAsync()
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

    private static bool ContainsInsensitive(string? source, string term)
    {
        return !string.IsNullOrEmpty(source) &&
               source.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
