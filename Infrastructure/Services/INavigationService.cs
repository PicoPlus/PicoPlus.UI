using Microsoft.AspNetCore.Components;

namespace PicoPlus.Infrastructure.Services;

/// <summary>
/// Abstraction for navigation operations
/// </summary>
public interface INavigationService
{
    void NavigateTo(string uri, bool forceLoad = false);
    void NavigateTo(string uri, NavigationOptions options);
}

/// <summary>
/// Implementation of navigation service
/// </summary>
public class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public void NavigateTo(string uri, bool forceLoad = false)
    {
        _navigationManager.NavigateTo(uri, forceLoad);
    }

    public void NavigateTo(string uri, NavigationOptions options)
    {
        _navigationManager.NavigateTo(uri, options);
    }
}
