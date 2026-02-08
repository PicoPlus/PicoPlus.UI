using Microsoft.AspNetCore.Components;
using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Services;

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
