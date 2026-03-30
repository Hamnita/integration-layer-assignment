using IntegrationLayer.Api.Clients;
using IntegrationLayer.Api.Controllers;
using IntegrationLayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class GatewayInsuranceControllerPersonTests
{
    private readonly IInsuranceServiceClient _client = Substitute.For<IInsuranceServiceClient>();
    private readonly InsuranceController _sut;

    public GatewayInsuranceControllerPersonTests()
    {
        _sut = new InsuranceController(_client);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsNotFound_WhenClientReturnsNull()
    {
        _client.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((PersonInsurancesModel?)null);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsOk_WithModel()
    {
        var model = new PersonInsurancesModel
        {
            PersonalIdentificationNumber = "199001011234",
            Insurances = [],
            TotalMonthlyCost = 0m,
        };
        _client.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(model);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }
}
