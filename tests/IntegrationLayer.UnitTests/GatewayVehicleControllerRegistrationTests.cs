using IntegrationLayer.Api.Clients;
using IntegrationLayer.Api.Controllers;
using IntegrationLayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class GatewayVehicleControllerRegistrationTests
{
    private readonly IVehicleServiceClient _client = Substitute.For<IVehicleServiceClient>();
    private readonly VehicleController _sut;

    public GatewayVehicleControllerRegistrationTests()
    {
        _sut = new VehicleController(_client);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsOk_WithModelFromClient()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _client.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("ABC123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsNotFound_WhenClientReturnsNull()
    {
        _client.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistration("ZZZ999", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
