using System.Net;
using System.Net.Http.Json;
using IntegrationLayer.Api.Clients;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.UnitTests;

public class GatewayVehicleServiceClientTests
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

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsModel_WhenServiceReturns200()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonContent.Create(model));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        var sut = new VehicleServiceClient(httpClient);

        var result = await sut.GetByRegistrationAsync("ABC123");

        Assert.NotNull(result);
        Assert.Equal("ABC123", result.RegistrationNumber);
    }
}

internal class FakeHttpMessageHandler(HttpStatusCode statusCode, HttpContent? content = null) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(statusCode) { Content = content ?? new StringContent("") });
    }
}
