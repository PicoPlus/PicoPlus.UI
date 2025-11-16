# HubSpot Contact Search API - Usage Examples

This document provides examples for using the updated `Contact.Search` and `Contact.SearchAdvanced` methods with the latest HubSpot API v3 format.

## ?? Table of Contents
- [Basic Search](#basic-search)
- [Advanced Search](#advanced-search)
- [Search Operators](#search-operators)
- [Pagination](#pagination)
- [Sorting](#sorting)
- [Common Use Cases](#common-use-cases)

---

## Basic Search

The `Search` method is ideal for simple queries with a single filter:

### Example 1: Search by National Code
```csharp
var contact = await _contactService.Search(
    query: "",
    paramName: "natcode",
    paramValue: "0923889698",
    propertiesToInclude: new[] { 
        "firstname", 
        "lastname", 
        "phone", 
        "email", 
        "natcode" 
    }
);
```

### Example 2: Search by Email
```csharp
var contact = await _contactService.Search(
    query: "",
    paramName: "email",
    paramValue: "user@example.com",
    propertiesToInclude: new[] { "firstname", "lastname", "email" }
);
```

### Example 3: Search with Pagination
```csharp
var firstPage = await _contactService.Search(
    query: "",
    paramName: "natcode",
    paramValue: "0923889698",
    propertiesToInclude: new[] { "firstname", "lastname" },
    limit: 50
);

// Get next page using 'after' token from response
if (firstPage.paging?.next?.after != null)
{
    var secondPage = await _contactService.Search(
        query: "",
        paramName: "natcode",
        paramValue: "0923889698",
        propertiesToInclude: new[] { "firstname", "lastname" },
        limit: 50,
        after: firstPage.paging.next.after
    );
}
```

### Example 4: Search with Sorting
```csharp
var contacts = await _contactService.Search(
    query: "",
    paramName: "natcode",
    paramValue: "0923889698",
    propertiesToInclude: new[] { "firstname", "lastname", "createdate" },
    sorts: new[] { "createdate" } // Sort by creation date ascending
);
```

---

## Advanced Search

The `SearchAdvanced` method supports multiple filters and complex queries:

### Example 1: Multiple Filter Conditions (AND)
```csharp
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "natcode",
        Value = "0923889698",
        Operator = "EQ"
    },
    new ContactFilter
    {
        PropertyName = "isverifiedbycr",
        Value = "true",
        Operator = "EQ"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { 
        "firstname", 
        "lastname", 
        "natcode", 
        "isverifiedbycr" 
    }
);
```

### Example 2: Date Range Search
```csharp
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "createdate",
        Value = "2024-01-01", // Start date
        HighValue = "2024-12-31", // End date
        Operator = "BETWEEN"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "createdate" }
);
```

### Example 3: Search with IN Operator (Multiple Values)
```csharp
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "contact_plan",
        Values = new[] { "gold", "platinum", "diamond" },
        Operator = "IN"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "contact_plan" }
);
```

### Example 4: Greater Than / Less Than
```csharp
// Find contacts with wallet balance > 1000000
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "wallet",
        Value = "1000000",
        Operator = "GT"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "wallet" }
);
```

### Example 5: Check Property Existence
```csharp
// Find contacts that have a phone number set
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "phone",
        Operator = "HAS_PROPERTY"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "phone" }
);
```

### Example 6: Text Search with Query
```csharp
// Free text search across all searchable properties
var contacts = await _contactService.SearchAdvanced(
    query: "????", // Search for "Ahmad" in Persian
    propertiesToInclude: new[] { "firstname", "lastname", "phone" },
    limit: 50
);
```

---

## Search Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `EQ` | Equal to | `natcode = "0923889698"` |
| `NEQ` | Not equal to | `contact_plan != "free"` |
| `LT` | Less than | `wallet < 500000` |
| `LTE` | Less than or equal | `wallet <= 500000` |
| `GT` | Greater than | `wallet > 1000000` |
| `GTE` | Greater than or equal | `wallet >= 1000000` |
| `BETWEEN` | Between two values | `createdate BETWEEN "2024-01-01" AND "2024-12-31"` |
| `IN` | In list of values | `contact_plan IN ["gold", "platinum"]` |
| `NOT_IN` | Not in list | `contact_plan NOT_IN ["free", "trial"]` |
| `HAS_PROPERTY` | Property has any value | Contact has phone number |
| `NOT_HAS_PROPERTY` | Property is empty/null | Contact has no email |
| `CONTAINS_TOKEN` | Contains substring | Name contains "Ahmad" |
| `NOT_CONTAINS_TOKEN` | Does not contain substring | Name doesn't contain "Test" |

---

## Pagination

HubSpot returns paginated results with a maximum of 100 records per request:

```csharp
// Helper method to get all contacts with pagination
public async Task<List<Contact.Search.Response.Result>> GetAllContactsAsync(
    List<ContactFilter> filters,
    string[] properties)
{
    var allContacts = new List<Contact.Search.Response.Result>();
    string? after = null;

    do
    {
        var response = await _contactService.SearchAdvanced(
            filters: filters,
            propertiesToInclude: properties,
            limit: 100,
            after: after
        );

        if (response.results != null)
        {
            allContacts.AddRange(response.results);
        }

        after = response.paging?.next?.after;

    } while (!string.IsNullOrEmpty(after));

    return allContacts;
}
```

---

## Sorting

Sort results by one or more properties:

```csharp
// Single sort
var contacts = await _contactService.SearchAdvanced(
    sorts: new[] { "createdate" }, // Ascending by default
    propertiesToInclude: new[] { "firstname", "lastname", "createdate" }
);

// Multiple sorts
var contacts = await _contactService.SearchAdvanced(
    sorts: new[] { "-wallet", "lastname" }, // Descending wallet, then ascending lastname
    propertiesToInclude: new[] { "firstname", "lastname", "wallet" }
);
```

**Sort syntax:**
- Ascending: `"propertyname"`
- Descending: `"-propertyname"` (prefix with minus sign)

---

## Common Use Cases

### Use Case 1: Find Verified Contacts with High Wallet Balance
```csharp
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "isverifiedbycr",
        Value = "true",
        Operator = "EQ"
    },
    new ContactFilter
    {
        PropertyName = "wallet",
        Value = "5000000",
        Operator = "GTE"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { 
        "firstname", 
        "lastname", 
        "wallet", 
        "phone" 
    },
    sorts: new[] { "-wallet" } // Highest balance first
);
```

### Use Case 2: Find Contacts Created This Month
```csharp
var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "createdate",
        Value = startOfMonth.ToString("yyyy-MM-dd"),
        HighValue = endOfMonth.ToString("yyyy-MM-dd"),
        Operator = "BETWEEN"
    }
};

var newContacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "createdate", "phone" }
);
```

### Use Case 3: Find Contacts by Birth Year
```csharp
// Find all contacts born in 1370 (Persian calendar)
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "dateofbirth",
        Value = "1370",
        Operator = "CONTAINS_TOKEN"
    }
};

var contacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { 
        "firstname", 
        "lastname", 
        "dateofbirth", 
        "natcode" 
    }
);
```

### Use Case 4: Find Unverified Contacts Without Phone
```csharp
var filters = new List<ContactFilter>
{
    new ContactFilter
    {
        PropertyName = "isverifiedbycr",
        Value = "false",
        Operator = "EQ"
    },
    new ContactFilter
    {
        PropertyName = "phone",
        Operator = "NOT_HAS_PROPERTY"
    }
};

var incompleteContacts = await _contactService.SearchAdvanced(
    filters: filters,
    propertiesToInclude: new[] { "firstname", "lastname", "email", "natcode" }
);
```

---

## Performance Tips

1. **Request only needed properties**: Don't request all properties if you only need a few
2. **Use pagination**: Always handle pagination for large result sets
3. **Cache results**: Consider caching frequently accessed data
4. **Use specific filters**: More specific filters = faster queries
5. **Limit result size**: Use the `limit` parameter appropriately (default: 100, max: 100)

---

## Error Handling

```csharp
try
{
    var contacts = await _contactService.SearchAdvanced(
        filters: filters,
        propertiesToInclude: properties
    );
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    // Invalid API token
    _logger.LogError("HubSpot authentication failed. Check API token.");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    // Rate limit exceeded
    _logger.LogWarning("HubSpot rate limit exceeded. Retry after delay.");
}
catch (TaskCanceledException ex)
{
    // Timeout
    _logger.LogError("HubSpot request timed out after {Timeout} seconds", 60);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error searching HubSpot contacts");
}
```

---

## API Reference

**HubSpot Documentation:**  
https://developers.hubspot.com/docs/api/crm/search

**Rate Limits:**
- 100 requests per 10 seconds (default)
- 4 requests per second per search endpoint

**Maximum Results:**
- 100 contacts per request
- Use pagination for larger datasets
