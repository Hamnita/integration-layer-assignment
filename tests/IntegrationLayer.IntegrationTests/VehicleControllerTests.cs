using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationLayer.IntegrationTests;

public class VehicleControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public VehicleControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
}
