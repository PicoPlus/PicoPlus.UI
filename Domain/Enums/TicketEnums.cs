#nullable enable

namespace NovinCRM.Domain.Enums;

/// <summary>
/// Lifecycle status for a support ticket.
/// Maps to HubSpot's hs_pipeline_stage values for the tickets pipeline.
/// </summary>
public enum TicketStatus
{
    Unknown,
    New,
    WaitingOnContact,
    WaitingOnUs,
    Closed
}

/// <summary>
/// Priority level for a support ticket.
/// </summary>
public enum TicketPriority
{
    Unknown,
    Low,
    Medium,
    High
}

/// <summary>
/// Type enumeration for CRM pipeline objects.
/// </summary>
public enum PipelineType
{
    Deals,
    Tickets
}
