#nullable enable

namespace PicoPlus.Domain.Entities;

/// <summary>
/// Domain entity representing a CRM contact (customer).
/// Contains only pure business fields — no HubSpot-specific IDs
/// or API property bags. The mapping from HubSpot DTOs to this
/// entity happens in the Infrastructure layer.
/// </summary>
public sealed record Contact
{
    // ── Identity ──────────────────────────────────────────────────────────
    /// <summary>Opaque external identifier (e.g. HubSpot contact ID).</summary>
    public required string Id { get; init; }

    public required string FirstName { get; init; }
    public required string LastName { get; init; }

    /// <summary>Iranian national code (کد ملی), 10 digits.</summary>
    public string? NationalCode { get; init; }

    // ── Contact ───────────────────────────────────────────────────────────
    public string? Phone { get; init; }
    public string? Email { get; init; }

    // ── Personal ──────────────────────────────────────────────────────────
    public string? DateOfBirth { get; init; }
    public string? FatherName { get; init; }
    public string? Gender { get; init; }

    // ── Verification ──────────────────────────────────────────────────────
    /// <summary>
    /// Shahkar verification status code.
    /// "100" = verified, "101" = not matched, "500" = error, "0" = not checked.
    /// </summary>
    public string? ShahkarStatus { get; init; }

    // ── Financial ─────────────────────────────────────────────────────────
    /// <summary>Wallet balance in the application's currency.</summary>
    public decimal? Wallet { get; init; }

    /// <summary>Cumulative revenue from all closed-won deals.</summary>
    public decimal? TotalRevenue { get; init; }

    // ── CRM Metadata ──────────────────────────────────────────────────────
    /// <summary>Number of associated deals (denormalized for quick display).</summary>
    public int? NumAssociatedDeals { get; init; }

    /// <summary>Current contact plan / subscription tier.</summary>
    public string? ContactPlan { get; init; }

    // ── Media ─────────────────────────────────────────────────────────────
    /// <summary>URL to the contact's avatar / national card image.</summary>
    public string? AvatarUrl { get; init; }

    // ── Derived behaviour ─────────────────────────────────────────────────
    /// <summary>Returns two-letter initials for avatar display.</summary>
    public string GetInitials()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            return $"{FirstName[0]}{LastName[0]}";
        return "U";
    }

    /// <summary>Returns the full name as a single string.</summary>
    public string GetFullName() => $"{FirstName} {LastName}".Trim();
}
