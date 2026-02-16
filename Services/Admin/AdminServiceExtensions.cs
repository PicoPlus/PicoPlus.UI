using PicoPlus.Models.CRM.Objects;
using PicoPlus.Services.CRM;
using DealModel = PicoPlus.Models.CRM.Objects.Deal;
using ContactModel = PicoPlus.Models.CRM.Objects.Contact;
using DealService = PicoPlus.Services.CRM.Objects.Deal;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;

namespace PicoPlus.Services.Admin;

/// <summary>
/// Extension methods and helper utilities for admin services
/// </summary>
public static class AdminServiceExtensions
{
    /// <summary>
    /// Get deals with pagination (wrapper for GetAll)
    /// </summary>
    public static async Task<List<DealModel.GetBatch.Response.Result>> GetBatchAsync(
        this DealService dealService,
        int limit = 100)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        try
        {
            var response = await dealService.GetAll(limit: limit);
            return response?.results ?? new List<DealModel.GetBatch.Response.Result>();
        }
        catch (Exception ex)
        {
            throw new AdminServiceOperationException(
                operation: "Deal.GetAll",
                context: $"limit={limit}",
                innerException: ex);
        }
    }

    /// <summary>
    /// Update deal stage
    /// </summary>
    public static async Task<bool> UpdateStageAsync(
        this DealService dealService,
        string dealId,
        string newStageId)
    {
        if (string.IsNullOrWhiteSpace(dealId))
        {
            throw new ArgumentException("Deal id is required.", nameof(dealId));
        }

        if (string.IsNullOrWhiteSpace(newStageId))
        {
            throw new ArgumentException("New stage id is required.", nameof(newStageId));
        }

        try
        {
            await dealService.Update(dealId, new { dealstage = newStageId });
            return true;
        }
        catch (Exception ex)
        {
            throw new AdminServiceOperationException(
                operation: "Deal.UpdateStage",
                context: $"dealId={dealId}, newStageId={newStageId}",
                innerException: ex);
        }
    }

    /// <summary>
    /// Get pipelines (wrapper method)
    /// </summary>
    public static async Task<PicoPlus.Models.CRM.Pipelines.List> GetAllAsync(this Pipelines pipelineService)
    {
        return await pipelineService.GetPipelines("deals");
    }

    /// <summary>
    /// Search contacts with default parameters
    /// </summary>
    public static async Task<ContactModel.Search.Response> SearchAsync(
        this ContactService contactService,
        int limit = 100,
        string query = "")
    {
        return await contactService.Search(
            query: query,
            paramName: "phone",
            paramValue: query,
            propertiesToInclude: new[]
            {
                "firstname", "lastname", "email", "phone", "natcode",
                "dateofbirth", "father_name", "gender", "shahkar_status",
                "wallet", "total_revenue", "num_associated_deals", "contact_plan",
                "company", "lifecyclestage", "hubspot_owner_id"
            },
            limit: limit);
    }
}

public sealed class AdminServiceOperationException : Exception
{
    public string Operation { get; }
    public string Context { get; }

    public AdminServiceOperationException(string operation, string context, Exception innerException)
        : base($"Admin service operation failed: {operation} ({context})", innerException)
    {
        Operation = operation;
        Context = context;
    }
}

/// <summary>
/// Helper class for property access
/// </summary>
public static class PropertyHelpers
{
    public static string? GetCompanyName(this DealModel.GetBatch.Response.Properties properties)
        => properties.company_name;

    public static string? GetContactName(this DealModel.GetBatch.Response.Properties properties)
        => properties.contact_name;

    public static string? GetCloseDate(this DealModel.GetBatch.Response.Properties properties)
        => properties.closedate;

    public static string? GetDescription(this DealModel.GetBatch.Response.Properties properties)
        => properties.description;

    public static string? GetOwnerId(this DealModel.GetBatch.Response.Properties properties)
        => properties.hubspot_owner_id;

    public static string? GetPipeline(this DealModel.GetBatch.Response.Properties properties)
        => properties.pipeline;

    public static string? GetCompany(this ContactModel.Search.Response.Result.Properties properties)
        => properties.company;

    public static string? GetLifecycleStage(this ContactModel.Search.Response.Result.Properties properties)
        => properties.lifecyclestage;

    public static string? GetHubspotOwnerId(this ContactModel.Search.Response.Result.Properties properties)
        => properties.hubspot_owner_id;
}
