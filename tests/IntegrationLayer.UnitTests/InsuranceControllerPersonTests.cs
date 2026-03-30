using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Controllers;
using IntegrationLayer.InsuranceService.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class InsuranceControllerPersonTests
{
    private readonly IInsuranceService _service = Substitute.For<IInsuranceService>();
    private readonly InsuranceController _sut;

    public InsuranceControllerPersonTests()
    {
        _sut = new InsuranceController(_service);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("ABCDEFGHIJKL")]
    [InlineData("199001011234X")]
    [InlineData("1990010112")]
    [InlineData("19900101-123")]
    public async Task GetByPersonId_ReturnsBadRequest_WhenFormatIsInvalid(string personId)
    {
        var result = await _sut.GetByPersonId(personId, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsNotFound_WhenPersonDoesNotExist()
    {
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((PersonInsurancesModel?)null);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsOk_WhenPersonExists()
    {
        var model = new PersonInsurancesModel
        {
            PersonalIdentificationNumber = "199001011234",
            Insurances = [],
            TotalMonthlyCost = 0m,
        };
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(model);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByPersonId_StripsDash_BeforeCallingService()
    {
        var model = new PersonInsurancesModel { PersonalIdentificationNumber = "199001011234" };
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(model);

        var result = await _sut.GetByPersonId("19900101-1234", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _service.Received(1).GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>());
    }
}
