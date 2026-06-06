namespace PicoPlus.Services.Shared;

/// <summary>
/// Shared builder for HubSpot API query parameters,
/// eliminating duplicated properties/associations loop logic.
/// </summary>
public static class HubSpotQueryBuilder
{
    /// <summary>
    /// Build a query string from properties and associations arrays.
    /// </summary>
    public static string BuildQueryString(
        string[]? properties = null,
        string[]? associations = null,
        string[]? defaultProperties = null,
        string[]? defaultAssociations = null,
        Dictionary<string, string>? extraParams = null)
    {
        var queryParams = new List<string>();

        var propsToUse = (properties != null && properties.Length > 0) ? properties : defaultProperties;
        if (propsToUse != null)
        {
            foreach (var prop in propsToUse)
            {
                queryParams.Add($"properties={prop}");
            }
        }

        var assocsToUse = (associations != null && associations.Length > 0) ? associations : defaultAssociations;
        if (assocsToUse != null)
        {
            foreach (var assoc in assocsToUse)
            {
                queryParams.Add($"associations={assoc}");
            }
        }

        if (extraParams != null)
        {
            foreach (var kvp in extraParams)
            {
                queryParams.Add($"{kvp.Key}={kvp.Value}");
            }
        }

        return queryParams.Count > 0 ? string.Join("&", queryParams) : string.Empty;
    }

    /// <summary>
    /// Build query string for paginated list endpoints (GetAll).
    /// </summary>
    public static string BuildPaginationQuery(int limit, string? after = null, string[]? properties = null)
    {
        var queryParams = new List<string> { $"limit={limit}" };

        if (!string.IsNullOrEmpty(after))
        {
            queryParams.Add($"after={after}");
        }

        if (properties != null && properties.Length > 0)
        {
            foreach (var prop in properties)
            {
                queryParams.Add($"properties={prop}");
            }
        }

        return string.Join("&", queryParams);
    }
}
