using PicoPlus.CleanArchitecture.Domain.Entities;

namespace PicoPlus.CleanArchitecture.Application.Ports;

public interface IAuthSessionPort
{
    Task StoreAuthenticatedUserAsync(User user, string role, CancellationToken cancellationToken = default);
}
