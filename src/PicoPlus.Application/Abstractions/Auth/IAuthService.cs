namespace PicoPlus.Application.Abstractions.Auth;

public interface IAuthService
{
    Task NavigateByRoleAsync(string role, bool persist = false, CancellationToken cancellationToken = default);
    Task<string> GetCurrentRoleAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAdminAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateRoleAccessAsync(string requiredRole, CancellationToken cancellationToken = default);
}
