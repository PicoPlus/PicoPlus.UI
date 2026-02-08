using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Services;

public class LocalStorageServiceWrapper : ILocalStorageService
{
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;

    public LocalStorageServiceWrapper(Blazored.LocalStorage.ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await _localStorage.SetItemAsync(key, value, cancellationToken);
    }

    public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _localStorage.GetItemAsync<T>(key, cancellationToken);
    }

    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        await _localStorage.RemoveItemAsync(key, cancellationToken);
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await _localStorage.ClearAsync(cancellationToken);
    }
}
