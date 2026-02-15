using PicoPlus.Application.Abstractions;
using PicoPlus.Infrastructure.Services;

namespace PicoPlus.Infrastructure.State;

public sealed class AuthSessionService(ISessionStorageService sessionStorageService) : IAuthSessionService
{
    public async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default)
    {
        var loginState = await sessionStorageService.GetItemAsync<int>("LogInState", cancellationToken);
        return loginState == 1;
    }
}
