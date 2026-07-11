namespace PicoPlus.Infrastructure.Services;

/// <summary>
/// Abstraction for session storage operations
/// </summary>
public interface ISessionStorageService
{
    Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation wrapping Blazored.SessionStorage
/// </summary>
public class SessionStorageServiceWrapper : ISessionStorageService
{
    private readonly Blazored.SessionStorage.ISessionStorageService _sessionStorage;

    public SessionStorageServiceWrapper(Blazored.SessionStorage.ISessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _sessionStorage.GetItemAsync<T>(key, cancellationToken);
    }

    public async Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await _sessionStorage.SetItemAsync(key, value, cancellationToken);
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        await _sessionStorage.RemoveItemAsync(key, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _sessionStorage.ClearAsync(cancellationToken);
    }
}
