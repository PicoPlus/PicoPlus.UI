using Microsoft.AspNetCore.Components;

namespace PicoPlus.Application.Abstractions.Services;

public interface INavigationService
{
    void NavigateTo(string uri, bool forceLoad = false);
    void NavigateTo(string uri, NavigationOptions options);
}
