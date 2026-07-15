using Microsoft.Extensions.Logging;
using NovinCRM.Models.CRM.Objects;
using NovinCRM.Services.CRM;
using DealModel        = NovinCRM.Models.CRM.Objects.Deal;
using ContactModel     = NovinCRM.Models.CRM.Objects.Contact;
using DealService      = NovinCRM.Services.CRM.Objects.Deal;
using ContactService   = NovinCRM.Services.CRM.Objects.Contact;
using PipelinesService = NovinCRM.Services.CRM.Pipelines;

namespace NovinCRM.Services.Admin;

/// <summary>
/// Extension methods and helper utilities for admin services
/// </summary>
public static class AdminServiceExtensions
{
    public static async Task<List<DealModel.GetBatch.Response.Result>> GetBatchAsync(
        this DealService dealService,
        int limit = 100,
        ILogger? logger = null)
    {
        try
        {
            var response = await dealService.GetAll(limit: limit);
            var results  = new List<DealModel.GetBatch.Response.Result>();

            if (response?.results != null)
            {
                foreach (var item in response.results)
                {
                    DateTime.TryParse(item.createdAt?.ToString(), out DateTime created);
                    DateTime.TryParse(item.updatedAt?.ToString(), out DateTime updated);
                    bool.TryParse(item.archived?.ToString(), out bool arch);

                    results.Add(new DealModel.GetBatch.Response.Result
                    {
                        id        = item.id?.ToString() ?? "",
                        createdAt = created,
                        updatedAt = updated,
                        archived  = arch,
                        properties = new DealModel.GetBatch.Response.Properties
                        {
                            amount              = item.properties?.amount?.ToString() ?? "0",
                            dealname            = item.properties?.dealname?.ToString() ?? "",
                            dealstage           = item.properties?.dealstage?.ToString() ?? "",
                            createdate          = item.properties?.createdate?.ToString() ?? "",
                            hs_lastmodifieddate = item.properties?.hs_lastmodifieddate?.ToString() ?? "",
                            hs_object_id        = item.properties?.hs_object_id?.ToString() ?? ""
                        }
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "AdminServiceExtensions.GetBatchAsync failed (limit={Limit})", limit);
            return new List<DealModel.GetBatch.Response.Result>();
        }
    }

    public static async Task<bool> UpdateStageAsync(
        this DealService dealService,
        string dealId,
        string newStageId,
        ILogger? logger = null)
    {
        try
        {
            await dealService.Update(dealId, new { dealstage = newStageId });
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "AdminServiceExtensions.UpdateStageAsync failed (dealId={DealId}, stage={Stage})",
                dealId, newStageId);
            return false;
        }
    }

    public static async Task<NovinCRM.Models.CRM.Pipelines.List> GetAllAsync(this PipelinesService pipelineService)
        => await pipelineService.GetPipelines("deals");

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
                "firstname", "lastname", "email", "phone", "ncode",
                "dateofbirth", "father_name", "gender", "shahkar_status",
                "wallet", "total_revenue", "num_associated_deals", "contact_plan"
            },
            limit: limit);
    }
}

public static class PropertyHelpers
{
    // Now delegates to the real DTO properties — stubs replaced by #72 (MP-9) fix
    public static string GetCompanyName(this DealModel.GetBatch.Response.Properties? p)  => string.Empty; // not in GetBatch response; fetched via association
    public static string GetContactName(this DealModel.GetBatch.Response.Properties? p)  => string.Empty; // not in GetBatch response; fetched via association
    public static string GetCloseDate(this DealModel.GetBatch.Response.Properties? p)    => p?.closedate ?? string.Empty;
    public static string GetDescription(this DealModel.GetBatch.Response.Properties? p)  => p?.description ?? string.Empty;
    public static string GetOwnerId(this DealModel.GetBatch.Response.Properties? p)      => p?.hubspot_owner_id ?? string.Empty;
    public static string GetPipeline(this DealModel.GetBatch.Response.Properties? p)     => p?.pipeline ?? string.Empty;

    public static string GetCompany(this ContactModel.Search.Response.Result.Properties? p)        => p?.company ?? string.Empty;
    public static string GetLifecycleStage(this ContactModel.Search.Response.Result.Properties? p) => p?.lifecyclestage ?? string.Empty;
    public static string GetHubspotOwnerId(this ContactModel.Search.Response.Result.Properties? p) => p?.hubspot_owner_id ?? string.Empty;
}
