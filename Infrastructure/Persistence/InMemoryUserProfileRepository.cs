using PicoPlus.Application.Abstractions;
using PicoPlus.Domain.Users;

namespace PicoPlus.Infrastructure.Persistence;

public sealed class InMemoryUserProfileRepository : IUserProfileRepository
{
    private static readonly UserProfile DemoUser = new()
    {
        Id = "demo-user",
        FullName = "Demo User",
        Mobile = "09120000000"
    };

    public Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(id == DemoUser.Id ? DemoUser : null);
    }
}
