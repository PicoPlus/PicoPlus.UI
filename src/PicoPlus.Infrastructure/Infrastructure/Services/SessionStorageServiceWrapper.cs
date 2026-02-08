using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Services;

public class SessionStorageServiceWrapper : ISessionStorageService
{
    private readonly Blazored.SessionStorage.ISessionStorageService _sessionStorage;

    public SessionStorageServiceWrapper(Blazored.SessionStorage.ISessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await _sessionStorage.SetItemAsync(key, value, cancellationToken);
    }

    public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _sessionStorage.GetItemAsync<T>(key, cancellationToken);
    }

    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        await _sessionStorage.RemoveItemAsync(key, cancellationToken);
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await _sessionStorage.ClearAsync(cancellationToken);
    }
}
