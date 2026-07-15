#nullable enable

using NovinCRM.Domain.Enums;

namespace NovinCRM.Domain.Entities;

/// <summary>
/// Domain entity representing a sales deal.
/// Contains only pure business fields — no HubSpot property bags.
/// The pipeline string is kept as an opaque identifier so the domain
/// does not need to know HubSpot pipeline GUIDs.
/// </summary>
public sealed record Deal
{
    // ── Identity ──────────────────────────────────────────────────────────
    /// <summary>Opaque external identifier (e.g. HubSpot deal ID).</summary>
    public required string Id { get; init; }

    public required string DealName { get; init; }

    // ── Financial ─────────────────────────────────────────────────────────
    public decimal Amount { get; init; }

    // ── Pipeline / stage ──────────────────────────────────────────────────
    public DealStage Stage { get; init; }

    /// <summary>
    /// Opaque pipeline identifier string (e.g. HubSpot pipeline ID).
    /// The Infrastructure layer is responsible for resolving this to
    /// a human-readable name when required by the UI.
    /// </summary>
    public string? Pipeline { get; init; }

    // ── Dates ─────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CloseDate { get; init; }
}
