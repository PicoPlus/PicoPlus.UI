using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace NovinCRM.Tests.Integration.Health;

/// <summary>
/// Integration tests for the health check endpoints.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to spin up the app in-process.
/// </summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("HubSpot:Token", "test-token-integration");
            b.UseSetting("IPPanel:Patterns:OTP", "test-pattern");
        }).CreateClient();
    }

    [Fact]
    public async Task LivenessEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
