namespace NovinCRM.Domain.Entities;

/// <summary>
/// Single-use access token for the customer invoice / feedback page.
/// Issued when a deal closes (or changes to a configured stage) and sent via SMS.
/// Expires after a configurable TTL and is marked consumed after the customer submits feedback.
/// </summary>
public sealed class InvoiceAccessToken
{
    public required string   Token      { get; init; }   // URL-safe random token
    public required string   DealId     { get; init; }
    public required string   ContactId  { get; init; }
    public required DateTime ExpiresAt  { get; init; }
    public bool      IsConsumed { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
