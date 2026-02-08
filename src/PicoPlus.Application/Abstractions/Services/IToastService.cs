namespace PicoPlus.Application.Abstractions.Services;

public interface IToastService
{
    event Action<string, string, ToastType, int>? OnShow;

    void ShowToast(string title, string message, ToastType type, int durationMs = 5000);
    void ShowSuccess(string title, string message);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
    void ShowWarning(string title, string message);
}

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}
