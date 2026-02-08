#nullable enable

namespace PicoPlus.Application.Dto.UserPanel;

/// <summary>
/// Immutable aggregate DTO for complete user panel state
/// </summary>
public sealed record UserPanelState
{
    public required ContactInfo Contact { get; init; }
    public required UserStatistics Statistics { get; init; }
    public required IReadOnlyList<DealSummary> Deals { get; init; }

    public static UserPanelState Empty => new()
    {
        Contact = new ContactInfo
        {
            Id = string.Empty,
            FirstName = "?????",
            LastName = "??????"
        },
        Statistics = new UserStatistics(),
        Deals = Array.Empty<DealSummary>()
    };
}
