namespace PicoPlus.Domain.Users;

public sealed class UserProfile
{
    public required string Id { get; init; }

    public required string FullName { get; init; }

    public required string Mobile { get; init; }

    public bool IsProfileCompleted => !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Mobile);
}
