namespace PicoPlus.Application.Abstractions.Services;

public interface ILocalStorageService
{
    ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
