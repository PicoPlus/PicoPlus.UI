using PicoPlus.CleanArchitecture.Domain.Entities;

namespace PicoPlus.CleanArchitecture.Application.Ports;

public interface IUserDataEnrichmentPort
{
    Task<User> EnrichAsync(User user, CancellationToken cancellationToken = default);
}
