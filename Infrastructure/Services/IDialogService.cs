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
/// Pure Blazor dialog service - NO JAVASCRIPT
/// </summary>
public class DialogService
{
    public event Action<DialogState>? OnShow;

    public Task<bool> ShowMessageBoxAsync(string title, string message, string yesText = "OK", string? noText = null)
    {
        var dialog = new DialogState
        {
            Title = title,
            Message = message,
            YesText = yesText,
            NoText = noText,
            IsConfirmation = !string.IsNullOrEmpty(noText),
            Type = DialogType.Info,
            TaskCompletionSource = new TaskCompletionSource<bool>()
        };

        OnShow?.Invoke(dialog);
        return dialog.TaskCompletionSource.Task;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        var dialog = new DialogState
        {
            Title = title,
            Message = message,
            YesText = "تایید",
            IsConfirmation = false,
            Type = DialogType.Error,
            TaskCompletionSource = new TaskCompletionSource<bool>()
        };

        OnShow?.Invoke(dialog);
        return dialog.TaskCompletionSource.Task;
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        var dialog = new DialogState
        {
            Title = title,
            Message = message,
            YesText = "تایید",
            IsConfirmation = false,
            Type = DialogType.Success,
            TaskCompletionSource = new TaskCompletionSource<bool>()
        };

        OnShow?.Invoke(dialog);
        return dialog.TaskCompletionSource.Task;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        var dialog = new DialogState
        {
            Title = title,
            Message = message,
            YesText = "تایید",
            IsConfirmation = false,
            Type = DialogType.Info,
            TaskCompletionSource = new TaskCompletionSource<bool>()
        };

        OnShow?.Invoke(dialog);
        return dialog.TaskCompletionSource.Task;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        var dialog = new DialogState
        {
            Title = title,
            Message = message,
            YesText = "تایید",
            IsConfirmation = false,
            Type = DialogType.Warning,
            TaskCompletionSource = new TaskCompletionSource<bool>()
        };

        OnShow?.Invoke(dialog);
        return dialog.TaskCompletionSource.Task;
    }
}

/// <summary>
/// Dialog service wrapper implementing IDialogService interface
/// </summary>
public class DialogServiceWrapper : IDialogService
{
    private readonly DialogService _dialogService;
    private readonly ToastService _toastService;

    public DialogServiceWrapper(DialogService dialogService, ToastService toastService)
    {
        _dialogService = dialogService;
        _toastService = toastService;
    }

    public async Task<bool?> ShowMessageBoxAsync(string title, string message, string yesText = "OK", string? noText = null, string? cancelText = null)
    {
        var result = await _dialogService.ShowMessageBoxAsync(title, message, yesText, noText);
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

public enum DialogType
{
    Info,
    Success,
    Error,
    Warning
}

public class DialogState
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string YesText { get; set; } = string.Empty;
    public string? NoText { get; set; }
    public bool IsConfirmation { get; set; }
    public DialogType Type { get; set; }
    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = new();
}
