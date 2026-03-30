using IntegrationLayer.Core.Models;
using IntegrationLayer.VehicleService.Controllers;
using IntegrationLayer.VehicleService.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class VehicleControllerRegistrationTests
{
    private readonly IVehicleService _service = Substitute.For<IVehicleService>();
    private readonly VehicleController _sut;

    public VehicleControllerRegistrationTests()
    {
        _sut = new VehicleController(_service);
    }

    [Theory]
    [InlineData("AB1234")]   // too many digits
    [InlineData("ABCD12")]   // too many letters
    [InlineData("123ABC")]   // wrong order
    [InlineData("AB")]       // too short
    [InlineData("ABC12")]    // only 2 digits
    [InlineData("ABC1234")]  // 4 digits
    public async Task GetByRegistration_ReturnsBadRequest_WhenFormatInvalid(string regNr)
    {
        var result = await _sut.GetByRegistration(regNr, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsNotFound_WhenNotInMock()
    {
        _service.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistration("ZZZ999", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsOk_WithModel_WhenFound()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _service.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("ABC123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByRegistration_NormalizesInputToUppercase_BeforeCallingService()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _service.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("abc123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
        await _service.Received(1).GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>());
    }
}
