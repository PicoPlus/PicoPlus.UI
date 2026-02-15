using PicoPlus.Application.Abstractions;
using PicoPlus.Domain.Common;
using PicoPlus.Domain.Users;

namespace PicoPlus.Application.Users;

public sealed class GetUserProfileUseCase(IUserProfileRepository repository)
{
    public async Task<Result<UserProfile>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<UserProfile>.Failure("User id is required.");
        }

        var profile = await repository.GetByIdAsync(userId, cancellationToken);
        return profile is null
            ? Result<UserProfile>.Failure("User profile not found.")
            : Result<UserProfile>.Success(profile);
    }
}
