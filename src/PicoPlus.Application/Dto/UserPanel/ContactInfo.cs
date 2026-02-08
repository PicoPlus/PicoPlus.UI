#nullable enable

namespace PicoPlus.Application.Dto.UserPanel;

/// <summary>
/// Immutable DTO for contact information displayed in user panel
/// </summary>
public sealed record ContactInfo
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? NationalCode { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? DateOfBirth { get; init; }
    public string? FatherName { get; init; }
    public decimal? Wallet { get; init; }

    /// <summary>
    /// Get user initials for avatar display
    /// </summary>
    public string GetInitials()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
        {
            return $"{FirstName[0]}{LastName[0]}";
        }
        return "U";
    }

    /// <summary>
    /// Get full name
    /// </summary>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }
}
