using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Services;

/// <summary>
/// Service for managing toast notifications
/// </summary>
public class ToastService : IToastService
{
    public event Action<string, string, ToastType, int>? OnShow;

    public void ShowToast(string title, string message, ToastType type, int durationMs = 5000)
    {
        OnShow?.Invoke(title, message, type, durationMs);
    }

    public void ShowSuccess(string title, string message)
    {
        ShowToast(title, message, ToastType.Success, 4000);
    }

    public void ShowError(string title, string message)
    {
        ShowToast(title, message, ToastType.Error, 6000);
    }

    public void ShowInfo(string title, string message)
    {
        ShowToast(title, message, ToastType.Info, 5000);
    }

    public void ShowWarning(string title, string message)
    {
        ShowToast(title, message, ToastType.Warning, 5000);
    }
}
