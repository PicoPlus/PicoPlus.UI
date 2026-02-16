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
        try
        {
            var response = await dealService.GetAll(limit: limit);
            return response?.results ?? new List<DealModel.GetBatch.Response.Result>();
        }
        catch
        {
            return new List<DealModel.GetBatch.Response.Result>();
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
        try
        {
            await dealService.Update(dealId, new { dealstage = newStageId });
            return true;
        }
        catch
        {
            return false;
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
                "wallet", "total_revenue", "num_associated_deals", "contact_plan"
            },
            limit: limit);
    }
}

/// <summary>
/// Helper class for property access with fallbacks
/// </summary>
public static class PropertyHelpers
{
    public static string GetCompanyName(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since company_name doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetContactName(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since contact_name doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetCloseDate(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since closedate doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetDescription(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since description doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetOwnerId(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since hubspot_owner_id doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetPipeline(this DealModel.GetBatch.Response.Properties properties)
    {
        // Since pipeline doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetCompany(this ContactModel.Search.Response.Result.Properties properties)
    {
        // Since company doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetLifecycleStage(this ContactModel.Search.Response.Result.Properties properties)
    {
        // Since lifecyclestage doesn't exist in the model, return empty
        return string.Empty;
    }

    public static string GetHubspotOwnerId(this ContactModel.Search.Response.Result.Properties properties)
    {
        // Since hubspot_owner_id doesn't exist in the model, return empty
        return string.Empty;
    }
}
