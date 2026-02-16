using PicoPlus.Models.CRM.Objects;
using PicoPlus.Services.Admin;

namespace PicoPlus.Tests;

public class PropertyHelpersTests
{
    [Fact]
    public void GetOwnerId_ReturnsMappedValue()
    {
        var properties = new Deal.GetBatch.Response.Properties
        {
            hubspot_owner_id = "12345"
        };

        var ownerId = properties.GetOwnerId();

        Assert.Equal("12345", ownerId);
    }

    [Fact]
    public void GetCompany_ReturnsMappedValue()
    {
        var properties = new Contact.Search.Response.Result.Properties
        {
            company = "PicoPlus"
        };

        var company = properties.GetCompany();

        Assert.Equal("PicoPlus", company);
    }
}
