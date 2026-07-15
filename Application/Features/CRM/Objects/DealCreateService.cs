#nullable enable

using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Entities;
using NovinCRM.Domain.Enums;
using NovinCRM.Domain.Events.Deal;
using NovinCRM.Domain.Extensions;
using NovinCRM.Domain.ValueObjects;
using NovinCRM.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace NovinCRM.Services.Deal;

/// <summary>
/// Create-deal dialog logic as a plain Application-layer service.
/// </summary>
public class DealCreateService : IDealCreateService
{
    private readonly IDealRepository     _dealRepo;
    private readonly IPipelineRepository _pipelineRepo;
    private readonly IOwnerRepository    _ownerRepo;
    private readonly ILineItemRepository _lineItemRepo;
    private readonly IDialogService      _dialogService;
    private readonly ILogger<DealCreateService> _logger;

    // ── Bound state ─────────────────────────────────────────────────────────
    public string? ContactId { get; set; }
    public string  DealName  { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  SelectedPipelineId
    {
        get => _selectedPipelineId;
        set { _selectedPipelineId = value; RefreshStages(); }
    }
    public string  SelectedDealStage  { get; set; } = string.Empty;
    public string  SelectedOwnerId    { get; set; } = string.Empty;
    public decimal? Amount    { get; set; }
    public decimal? TaxAmount { get; set; }
    public DateTime? CloseDate { get; set; }
    public string? DealType  { get; set; }
    public string? Priority  { get; set; }

    public IReadOnlyList<Pipeline>      Pipelines  { get; private set; } = Array.Empty<Pipeline>();
    public IReadOnlyList<PipelineStage> DealStages { get; private set; } = Array.Empty<PipelineStage>();
    public IReadOnlyList<Owner>         Owners     { get; private set; } = Array.Empty<Owner>();

    public List<Models.CRM.Commerce.LineItem.Create.Request.Input> LineItems { get; set; } = new();

    public bool   IsLoading    { get; private set; }
    public string? ErrorMessage { get; private set; }

    private string _selectedPipelineId = string.Empty;

    private readonly IDomainEventDispatcher _eventDispatcher;

    public DealCreateService(
        IDealRepository dealRepo,
        IPipelineRepository pipelineRepo,
        IOwnerRepository ownerRepo,
        ILineItemRepository lineItemRepo,
        IDialogService dialogService,
        IDomainEventDispatcher eventDispatcher,
        ILogger<DealCreateService> logger)
    {
        _dealRepo        = dealRepo;
        _pipelineRepo    = pipelineRepo;
        _ownerRepo       = ownerRepo;
        _lineItemRepo    = lineItemRepo;
        _dialogService   = dialogService;
        _eventDispatcher = eventDispatcher;
        _logger          = logger;
    }

    public async Task InitializeAsync(string? contactId, CancellationToken ct = default)
    {
        ContactId = contactId;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var pipTask    = _pipelineRepo.GetAllAsync("deals");
            var ownerTask  = _ownerRepo.GetAllAsync();
            await Task.WhenAll(pipTask, ownerTask);

            Pipelines = await pipTask;
            Owners    = await ownerTask;

            if (Pipelines.Any()) SelectedPipelineId = Pipelines.First().PipelineId;
            if (Owners.Any())    SelectedOwnerId    = Owners.First().Id;

            CloseDate = DateTime.Now.AddDays(30);
            LineItems = new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DealCreateService.InitializeAsync error");
            ErrorMessage = $"خطا در مقداردهی اولیه: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    public async Task<string?> CreateDealAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(DealName))        { ErrorMessage = "نام معامله الزامی است";         return null; }
        if (string.IsNullOrWhiteSpace(SelectedPipelineId)){ ErrorMessage = "انتخاب پایپ‌لاین ضروری است";   return null; }
        if (string.IsNullOrWhiteSpace(SelectedDealStage)) { ErrorMessage = "انتخاب مرحله معامله ضروری است"; return null; }
        if (string.IsNullOrWhiteSpace(SelectedOwnerId))   { ErrorMessage = "انتخاب مسئول معامله ضروری است"; return null; }

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            IEnumerable<string> lineItemIds = Array.Empty<string>();
            if (LineItems.Any())
            {
                var domainItems = LineItems.Select(li => new LineItem
                {
                    Id                 = string.Empty,
                    Name               = li.properties?.name  ?? "محصول",
                    Price              = li.properties?.price  ?? 0m,
                    Quantity           = li.properties?.quantity ?? 1L,
                    ProductId          = li.properties?.hs_product_id,
                    Sku                = li.properties?.hs_sku,
                    DiscountPercentage = decimal.TryParse(li.properties?.hs_discount_percentage,
                                            out var dp) ? dp : 0m
                });
                lineItemIds = await _lineItemRepo.CreateBatchAsync(domainItems);
                Amount = LineItems.Sum(li => li.properties?.TotalPrice ?? 0m);
            }

            var deal = new Domain.Entities.Deal
            {
                Id       = string.Empty,
                DealName = DealName,
                Pipeline = SelectedPipelineId,
                Stage    = DealStageExtensions.ParseDealStage(SelectedDealStage),
                Amount   = Amount ?? 0m,
                CloseDate = CloseDate
            };

            var created = await _dealRepo.CreateAsync(deal, contactId: ContactId, lineItemIds: lineItemIds, stageName: SelectedDealStage);

            // Raise domain event — handled asynchronously by DealCreatedHandler.
            await _eventDispatcher.DispatchAsync(new DealCreatedEvent
            {
                DealId    = created.Id,
                DealName  = created.DealName,
                Stage     = created.Stage,
                Amount    = created.Amount,
                Pipeline  = created.Pipeline,
                ContactId = ContactId,
            }, ct);

            await _dialogService.ShowSuccessAsync("موفق", $"معامله با موفقیت ثبت شد. شناسه: {created.Id}");
            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DealCreateService.CreateDealAsync error");
            ErrorMessage = $"خطا در ایجاد معامله: {ex.Message}";
            await _dialogService.ShowErrorAsync("خطا", ErrorMessage);
            return null;
        }
        finally { IsLoading = false; }
    }

    public void Reset()
    {
        DealName = string.Empty; Description = null;
        SelectedDealStage = string.Empty; Amount = null; TaxAmount = null;
        CloseDate = DateTime.Now.AddDays(30); DealType = null; Priority = null;
        LineItems = new(); ErrorMessage = null;
        if (Pipelines.Any()) SelectedPipelineId = Pipelines.First().PipelineId;
        if (Owners.Any())    SelectedOwnerId    = Owners.First().Id;
    }

    private void RefreshStages()
    {
        if (string.IsNullOrEmpty(_selectedPipelineId)) { DealStages = Array.Empty<PipelineStage>(); SelectedDealStage = string.Empty; return; }
        var p = Pipelines.FirstOrDefault(x => x.PipelineId == _selectedPipelineId);
        DealStages = p?.Stages ?? Array.Empty<PipelineStage>();
        if (DealStages.Any()) SelectedDealStage = DealStages.First().StageId;
    }
}
