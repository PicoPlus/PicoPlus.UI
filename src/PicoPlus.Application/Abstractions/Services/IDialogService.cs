using Microsoft.JSInterop;

namespace PicoPlus.Application.Abstractions.Services;

public interface IDialogService
{
    ValueTask Alert(string message);
    ValueTask<bool> Confirm(string message);
    ValueTask<T?> Prompt<T>(string message, T defaultValue = default!);
}
