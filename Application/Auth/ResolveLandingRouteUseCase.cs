using PicoPlus.Application.Abstractions;

namespace PicoPlus.Application.Auth;

public sealed class ResolveLandingRouteUseCase(IAuthSessionService authSessionService)
{
    public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var isLoggedIn = await authSessionService.IsLoggedInAsync(cancellationToken);
        return isLoggedIn ? "/user" : "/auth/login";
    }
}
