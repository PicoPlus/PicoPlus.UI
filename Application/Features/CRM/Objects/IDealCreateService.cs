#nullable enable

using PicoPlus.Domain.Entities;
using PicoPlus.Domain.ValueObjects;

namespace PicoPlus.Services.Deal;

/// <summary>
/// Service for the Create Deal dialog.
/// Clean Architecture: depends only on Application interfaces and Domain types.
/// </summary>
public interface IDealCreateService
{
    // ── Bound state ─────────────────────────────────────────────────────────
    string? ContactId { get; set; }
    string  DealName  { get; set; }
    string? Description { get; set; }
    string  SelectedPipelineId { get; set; }
    string  SelectedDealStage  { get; set; }
    string  SelectedOwnerId    { get; set; }
    decimal? Amount    { get; set; }
    decimal? TaxAmount { get; set; }
    DateTime? CloseDate { get; set; }
    string? DealType  { get; set; }
    string? Priority  { get; set; }

    IReadOnlyList<Pipeline>      Pipelines  { get; }
    IReadOnlyList<PipelineStage> DealStages { get; }
    IReadOnlyList<Owner>         Owners     { get; }

    List<Models.CRM.Commerce.LineItem.Create.Request.Input> LineItems { get; set; }

    bool   IsLoading    { get; }
    string? ErrorMessage { get; }

    // ── Commands ────────────────────────────────────────────────────────────
    Task InitializeAsync(string? contactId, CancellationToken ct = default);
    Task<string?> CreateDealAsync(CancellationToken ct = default);
    void Reset();
}
