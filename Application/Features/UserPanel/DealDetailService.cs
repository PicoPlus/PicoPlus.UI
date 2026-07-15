#nullable enable

using Microsoft.Extensions.Logging;
using NovinCRM.Domain.Entities;
using NovinCRM.Models.CRM.Objects;
using NovinCRM.Services.CRM.Objects;
using NovinCRM.Domain.Enums;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using NovinCRM.Application.Common.Interfaces;
using DealService = NovinCRM.Services.CRM.Objects.Deal;
using NotesService = NovinCRM.Services.CRM.Engagements.Notes;
using LineItemService = NovinCRM.Services.CRM.Commerce.LineItem;
using DealModel = NovinCRM.Models.CRM.Objects.Deal;
using DealStage = NovinCRM.Domain.Enums.DealStage;
using NovinCRM.Domain.Extensions;

namespace NovinCRM.Services.UserPanel;

/// <summary>
/// Service to load comprehensive deal details including notes, files, and engagement logs.
/// </summary>
public class DealDetailService
{
    private readonly DealService _dealService;
    private readonly NotesService _notesService;
    private readonly LineItemService _lineItemService;
    private readonly IAssociateService _associate;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DealDetailService> _logger;
    private readonly string _hubSpotToken;

    public DealDetailService(
        DealService dealService,
        NotesService notesService,
        LineItemService lineItemService,
        IAssociateService associate,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DealDetailService> logger)
    {
        _dealService = dealService;
        _notesService = notesService;
        _lineItemService = lineItemService;
        _associate = associate;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured");
    }

    public class DealDetailViewModel
    {
        public required NovinCRM.Domain.Entities.Deal Deal { get; init; }
        public List<Note> Notes { get; init; } = [];
        public List<EngagementLog> CallLogs { get; init; } = [];
        public List<FileAttachment> Files { get; init; } = [];
        public List<InvoiceLineItem> LineItems { get; init; } = [];
        public string? ErrorMessage { get; init; }
    }

    public class InvoiceLineItem
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public decimal Price { get; init; }
        public long Quantity { get; init; }
        public decimal DiscountPct { get; init; }
        public string? Sku { get; init; }
        public decimal LineTotal => Price * Quantity * (1 - DiscountPct / 100m);
    }

    public class Note
    {
        public required string Id { get; init; }
        public required string Body { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required string CreatedBy { get; init; }
        public List<string> AttachmentIds { get; init; } = [];
    }

    public class EngagementLog
    {
        public required string Id { get; init; }
        public required string Type { get; init; } // call, email, meeting, etc.
        public required string Subject { get; init; }
        public required DateTime Timestamp { get; init; }
        public string? Description { get; init; }
        public string? Duration { get; init; }
        public string? Status { get; init; }
    }

    public class FileAttachment
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Url { get; init; }
        public required long SizeBytes { get; init; }
        public required DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Load complete deal details including notes and engagement logs.
    /// </summary>
    public async Task<DealDetailViewModel> LoadDealDetailsAsync(
        string dealId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading deal details for: {DealId}", dealId);

            // Load deal with notes association
            var dealResponse = await _dealService.GetDeal(
                dealId,
                properties: null,
                associations: new[] { "notes", "emails", "calls", "meetings" });

            if (dealResponse == null)
            {
                return new DealDetailViewModel
                {
                    Deal = new NovinCRM.Domain.Entities.Deal
                    {
                        Id = dealId,
                        DealName = "Unknown Deal"
                    },
                    ErrorMessage = "Could not load deal details from HubSpot"
                };
            }

            var domainDeal = MapToDomainDeal(dealResponse);

            // Load notes if available
            var notes = await LoadNotesAsync(dealResponse, cancellationToken);

            // Load engagement logs (calls, emails, meetings)
            var engagementLogs = await LoadEngagementLogsAsync(dealResponse, cancellationToken);

            var lineItems = await LoadLineItemsAsync(dealId, cancellationToken);

            return new DealDetailViewModel
            {
                Deal = domainDeal,
                Notes = notes,
                CallLogs = engagementLogs,
                LineItems = lineItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deal details for: {DealId}", dealId);
            return new DealDetailViewModel
            {
                Deal = new NovinCRM.Domain.Entities.Deal
                {
                    Id = dealId,
                    DealName = "Error Loading Deal"
                },
                ErrorMessage = $"Error loading deal details: {ex.Message}"
            };
        }
    }

    private NovinCRM.Domain.Entities.Deal MapToDomainDeal(DealModel.Get.Response dealModel)
    {
        var props = dealModel.properties;
        return new NovinCRM.Domain.Entities.Deal
        {
            Id = dealModel.id,
            DealName = props?.dealname ?? "Unknown",
            Amount = ParseDecimal(props?.amount),
            Stage = ParseStage(props?.dealstage),
            Pipeline = props?.pipeline,
            CreatedAt = dealModel.createdAt,
            UpdatedAt = dealModel.updatedAt,
            CloseDate = props?.closedate
        };
    }

    private async Task<List<Note>> LoadNotesAsync(
        DealModel.Get.Response dealModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var notes = new List<Note>();

            if (dealModel.associations?.notes?.results == null ||
                dealModel.associations.notes.results.Count == 0)
            {
                _logger.LogInformation("No notes found for deal: {DealId}", dealModel.id);
                return notes;
            }

            foreach (var noteAssoc in dealModel.associations.notes.results)
            {
                try
                {
                    var noteId = noteAssoc.id;
                    var noteDetails = await GetNoteDetailsAsync(noteId, cancellationToken);

                    if (noteDetails != null)
                    {
                        notes.Add(noteDetails);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading note details");
                }
            }

            return notes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notes for deal: {DealId}", dealModel.id);
            return [];
        }
    }

    private async Task<Note?> GetNoteDetailsAsync(string noteId, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.hubapi.com/crm/v3/objects/notes/{noteId}?properties=hs_note_body&properties=hs_attachment_ids&properties=hs_timestamp&properties=hs_created_by";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get note {NoteId}: {StatusCode}", noteId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var noteData = JsonSerializer.Deserialize<JsonElement>(json);

            if (!noteData.TryGetProperty("properties", out var props))
                return null;

            var body = props.TryGetProperty("hs_note_body", out var bodyProp)
                ? bodyProp.GetString() ?? ""
                : "";

            var attachmentIds = new List<string>();
            if (props.TryGetProperty("hs_attachment_ids", out var attachProp))
            {
                var idsStr = attachProp.GetString();
                if (!string.IsNullOrEmpty(idsStr))
                {
                    attachmentIds = idsStr.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
            }

            var createdAt = props.TryGetProperty("hs_timestamp", out var timestampProp)
                ? new DateTime(timestampProp.GetInt64() / 1000, DateTimeKind.Utc)
                : DateTime.UtcNow;

            var createdBy = props.TryGetProperty("hs_created_by", out var createdByProp)
                ? createdByProp.GetString() ?? "Unknown"
                : "Unknown";

            return new Note
            {
                Id = noteId,
                Body = body,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
                AttachmentIds = attachmentIds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching note {NoteId}", noteId);
            return null;
        }
    }

    private async Task<List<EngagementLog>> LoadEngagementLogsAsync(
        DealModel.Get.Response dealModel,
        CancellationToken cancellationToken)
    {
        var logs = new List<EngagementLog>();

        try
        {
            // Load calls
            if (dealModel.associations?.notes?.results != null)
            {
                foreach (var call in dealModel.associations.notes.results)
                {
                    var callLog = await GetCallDetailsAsync(call.id, cancellationToken);
                    if (callLog != null)
                        logs.Add(callLog);
                }
            }

            // Load emails
            if (dealModel.associations?.notes?.results != null)
            {
                foreach (var email in dealModel.associations.notes.results)
                {
                    var emailLog = await GetEmailDetailsAsync(email.id, cancellationToken);
                    if (emailLog != null)
                        logs.Add(emailLog);
                }
            }

            // Load meetings
            if (dealModel.associations?.notes?.results != null)
            {
                foreach (var meeting in dealModel.associations.notes.results)
                {
                    var meetingLog = await GetMeetingDetailsAsync(meeting.id, cancellationToken);
                    if (meetingLog != null)
                        logs.Add(meetingLog);
                }
            }

            // Sort by timestamp descending (most recent first)
            return logs.OrderByDescending(x => x.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading engagement logs for deal: {DealId}", dealModel.id);
            return logs;
        }
    }

    private async Task<EngagementLog?> GetCallDetailsAsync(string callId, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.hubapi.com/crm/v3/objects/calls/{callId}?properties=hs_call_subject&properties=hs_timestamp&properties=hs_call_duration&properties=hs_call_status";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var callData = JsonSerializer.Deserialize<JsonElement>(json);

            if (!callData.TryGetProperty("properties", out var props))
                return null;

            var subject = props.TryGetProperty("hs_call_subject", out var subjectProp)
                ? subjectProp.GetString() ?? "Call"
                : "Call";

            var timestamp = props.TryGetProperty("hs_timestamp", out var timestampProp)
                ? new DateTime(timestampProp.GetInt64() / 1000, DateTimeKind.Utc)
                : DateTime.UtcNow;

            var duration = props.TryGetProperty("hs_call_duration", out var durationProp)
                ? durationProp.GetInt32().ToString()
                : null;

            var status = props.TryGetProperty("hs_call_status", out var statusProp)
                ? statusProp.GetString()
                : null;

            return new EngagementLog
            {
                Id = callId,
                Type = "call",
                Subject = subject,
                Timestamp = timestamp,
                Duration = duration,
                Status = status
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching call {CallId}", callId);
            return null;
        }
    }

    private async Task<EngagementLog?> GetEmailDetailsAsync(string emailId, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.hubapi.com/crm/v3/objects/emails/{emailId}?properties=hs_email_subject&properties=hs_timestamp";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var emailData = JsonSerializer.Deserialize<JsonElement>(json);

            if (!emailData.TryGetProperty("properties", out var props))
                return null;

            var subject = props.TryGetProperty("hs_email_subject", out var subjectProp)
                ? subjectProp.GetString() ?? "Email"
                : "Email";

            var timestamp = props.TryGetProperty("hs_timestamp", out var timestampProp)
                ? new DateTime(timestampProp.GetInt64() / 1000, DateTimeKind.Utc)
                : DateTime.UtcNow;

            return new EngagementLog
            {
                Id = emailId,
                Type = "email",
                Subject = subject,
                Timestamp = timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching email {EmailId}", emailId);
            return null;
        }
    }

    private async Task<EngagementLog?> GetMeetingDetailsAsync(string meetingId, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.hubapi.com/crm/v3/objects/meetings/{meetingId}?properties=hs_meeting_title&properties=hs_timestamp";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var meetingData = JsonSerializer.Deserialize<JsonElement>(json);

            if (!meetingData.TryGetProperty("properties", out var props))
                return null;

            var subject = props.TryGetProperty("hs_meeting_title", out var subjectProp)
                ? subjectProp.GetString() ?? "Meeting"
                : "Meeting";

            var timestamp = props.TryGetProperty("hs_timestamp", out var timestampProp)
                ? new DateTime(timestampProp.GetInt64() / 1000, DateTimeKind.Utc)
                : DateTime.UtcNow;

            return new EngagementLog
            {
                Id = meetingId,
                Type = "meeting",
                Subject = subject,
                Timestamp = timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching meeting {MeetingId}", meetingId);
            return null;
        }
    }

    private async Task<List<InvoiceLineItem>> LoadLineItemsAsync(
        string dealId, CancellationToken cancellationToken)
    {
        var result = new List<InvoiceLineItem>();
        try
        {
            var ids = await _associate.GetAssociatedIdsAsync(dealId, "deal", "line_item");
            foreach (var id in ids)
            {
                try
                {
                    var li = await _lineItemService.GetLineItem(id);
                    if (li?.properties == null) continue;
                    result.Add(new InvoiceLineItem
                    {
                        Id          = li.id ?? id,
                        Name        = li.properties.name ?? string.Empty,
                        Price       = decimal.TryParse(li.properties.price,    out var p) ? p : 0m,
                        Quantity    = long.TryParse(li.properties.quantity,    out var q) ? q : 1L,
                        DiscountPct = decimal.TryParse(li.properties.hs_discount_percentage, out var d) ? d : 0m,
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading line item {Id} for deal {DealId}", id, dealId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading line items for deal {DealId}", dealId);
        }
        return result;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return decimal.TryParse(value, out var result) ? result : 0;
    }

    private static DealStage ParseStage(string? value)
    {
        return value.ParseDealStage();
    }
}
