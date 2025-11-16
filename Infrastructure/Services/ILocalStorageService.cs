namespace PicoPlus.Infrastructure.Services;

using Blazored.LocalStorage;

/// <summary>
/// Abstraction for local storage operations (persists across browser restarts)
/// </summary>
public interface ILocalStorageService
{
    Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation wrapping Blazored.LocalStorage
/// </summary>
public class LocalStorageServiceWrapper : ILocalStorageService
{
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;

    public LocalStorageServiceWrapper(Blazored.LocalStorage.ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _localStorage.GetItemAsync<T>(key);
    }

    public async Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await _localStorage.SetItemAsync(key, value);
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        await _localStorage.RemoveItemAsync(key);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _localStorage.ClearAsync();
    }
}
