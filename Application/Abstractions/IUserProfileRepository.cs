using PicoPlus.Domain.Users;

namespace PicoPlus.Application.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
