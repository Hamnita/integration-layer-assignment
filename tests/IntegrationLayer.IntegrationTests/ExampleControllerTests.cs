using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationLayer.IntegrationTests;

public class ExampleControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExampleControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/example");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
