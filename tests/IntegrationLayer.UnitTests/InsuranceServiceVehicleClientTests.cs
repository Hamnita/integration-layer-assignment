using System.Net;
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Clients;

namespace IntegrationLayer.UnitTests;

public class InsuranceServiceVehicleClientTests
{
    [Fact]
    public async Task GetByRegistrationAsync_ReturnsNull_WhenServiceReturns404()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        var sut = new VehicleServiceClient(httpClient);

        var result = await sut.GetByRegistrationAsync("ABC123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_ThrowsHttpRequestException_WhenServiceReturns500()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        var sut = new VehicleServiceClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetByRegistrationAsync("ABC123"));
    }
}
