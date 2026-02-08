using Microsoft.JSInterop;
using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Services;

public class DialogServiceWrapper : IDialogService
{
    private readonly IJSRuntime _js;

    public DialogServiceWrapper(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask Alert(string message)
    {
        return _js.InvokeVoidAsync("alert", message);
    }

    public ValueTask<bool> Confirm(string message)
    {
        return _js.InvokeAsync<bool>("confirm", message);
    }

    public ValueTask<T?> Prompt<T>(string message, T defaultValue = default!)
    {
        return _js.InvokeAsync<T?>("prompt", message, defaultValue);
    }
}
