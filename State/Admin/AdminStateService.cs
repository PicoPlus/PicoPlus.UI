using PicoPlus.Models.Admin;
using PicoPlus.Infrastructure.Services;

namespace PicoPlus.State.Admin;

/// <summary>
/// State management for admin panel
/// </summary>
public class AdminStateService
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILogger<AdminStateService> _logger;

    private const string SELECTED_OWNER_KEY = "admin_selected_owner";
    private const string RECENT_OWNERS_KEY = "admin_recent_owners";

    public event Action? OnStateChanged;

    public AdminOwnerContext Context { get; private set; } = new();

    public AdminStateService(
        ISessionStorageService sessionStorage,
        ILogger<AdminStateService> logger)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    /// <summary>
    /// Initialize admin state from storage
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var selectedOwner = await _sessionStorage.GetItemAsync<HubSpotOwner>(SELECTED_OWNER_KEY, cancellationToken);
            var recentOwners = await _sessionStorage.GetItemAsync<List<HubSpotOwner>>(RECENT_OWNERS_KEY, cancellationToken);

            Context.SelectedOwner = selectedOwner;
            Context.RecentOwners = recentOwners ?? new();

            _logger.LogInformation("Admin state initialized. Selected owner: {Owner}", selectedOwner?.Email ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing admin state");
            Context = new AdminOwnerContext();
        }
    }

    /// <summary>
    /// Select an owner for admin operations
    /// </summary>
    public async Task SelectOwnerAsync(HubSpotOwner owner, CancellationToken cancellationToken = default)
    {
        try
        {
            Context.SelectedOwner = owner;
            Context.SelectedAt = DateTime.UtcNow;

            // Add to recent owners (keep last 5)
            Context.RecentOwners.RemoveAll(o => o.Id == owner.Id);
            Context.RecentOwners.Insert(0, owner);
            if (Context.RecentOwners.Count > 5)
            {
                Context.RecentOwners = Context.RecentOwners.Take(5).ToList();
            }

            // Save to storage
            await _sessionStorage.SetItemAsync(SELECTED_OWNER_KEY, owner, cancellationToken);
            await _sessionStorage.SetItemAsync(RECENT_OWNERS_KEY, Context.RecentOwners, cancellationToken);

            _logger.LogInformation("Selected owner: {Owner}", owner.Email);
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting owner");
        }
    }

    /// <summary>
    /// Clear selected owner
    /// </summary>
    public async Task ClearSelectedOwnerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Context.SelectedOwner = null;
            await _sessionStorage.RemoveItemAsync(SELECTED_OWNER_KEY, cancellationToken);
            
            _logger.LogInformation("Cleared selected owner");
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing selected owner");
        }
    }

    /// <summary>
    /// Get selected owner or null
    /// </summary>
    public HubSpotOwner? GetSelectedOwner() => Context.SelectedOwner;

    /// <summary>
    /// Check if an owner is selected
    /// </summary>
    public bool HasSelectedOwner() => Context.SelectedOwner != null;

    /// <summary>
    /// Notify subscribers of state change
    /// </summary>
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
