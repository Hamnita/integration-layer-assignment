using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.IntegrationTests;

public class ApiKeyMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "integration-test-key";
    private readonly WebApplicationFactory<Program> _factory;

    public ApiKeyMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKey"] = TestApiKey,
                    ["Services:VehicleService"] = "http://localhost:5200/",
                    ["Services:InsuranceService"] = "http://localhost:5300/",
                    ["Services:InternalApiKey"] = "internal-test-key"
                });
            });
        });
    }

    [Fact]
    public async Task Request_Returns401_WhenApiKeyHeaderMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_Returns401_WhenApiKeyHeaderIsWrong()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_DoesNotReturn401_WhenApiKeyHeaderIsCorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
