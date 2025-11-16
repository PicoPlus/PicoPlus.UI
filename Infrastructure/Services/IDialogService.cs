using Microsoft.JSInterop;

namespace PicoPlus.Infrastructure.Services;

/// <summary>
/// Abstraction for dialog operations
/// </summary>
public interface IDialogService
{
    Task<bool?> ShowMessageBoxAsync(string title, string message, string yesText = "OK", string? noText = null, string? cancelText = null);
    Task ShowErrorAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
}

/// <summary>
/// Toast-based dialog service implementation using modern notifications
/// </summary>
public class DialogServiceWrapper : IDialogService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ToastService _toastService;

    public DialogServiceWrapper(IJSRuntime jsRuntime, ToastService toastService)
    {
        _jsRuntime = jsRuntime;
        _toastService = toastService;
    }

    public async Task<bool?> ShowMessageBoxAsync(string title, string message, string yesText = "OK", string? noText = null, string? cancelText = null)
    {
        // For confirm dialogs, still use browser confirm
        var result = await _jsRuntime.InvokeAsync<bool>("confirm", $"{title}\n\n{message}");
        return result;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        _toastService.ShowError(title, message);
        return Task.CompletedTask;
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        _toastService.ShowSuccess(title, message);
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        _toastService.ShowInfo(title, message);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        _toastService.ShowWarning(title, message);
        return Task.CompletedTask;
    }
}
