using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;
using NSubstitute;
using InsuranceServiceImpl = IntegrationLayer.InsuranceService.Services.InsuranceService;

namespace IntegrationLayer.UnitTests;

public class InsuranceServicePersonTests
{
    private readonly IInsuranceRepository _repository = Substitute.For<IInsuranceRepository>();
    private readonly IVehicleServiceClient _vehicleClient = Substitute.For<IVehicleServiceClient>();
    private readonly InsuranceServiceImpl _sut;

    public InsuranceServicePersonTests()
    {
        _sut = new InsuranceServiceImpl(_repository, _vehicleClient);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsNull_WhenPersonNotFound()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((IEnumerable<PersonInsuranceEntry>?)null);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsPetInsurance_WithCorrectCost()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Pet, null) });

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        Assert.Equal("199001011234", result.PersonalIdentificationNumber);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Pet, insurance.Type);
        Assert.Equal(10m, insurance.MonthlyCost);
        Assert.Null(insurance.Vehicle);
        Assert.Equal(10m, result.TotalMonthlyCost);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsPersonalHealthInsurance_WithCorrectCost()
    {
        _repository.GetByPersonIdAsync("198505152345", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.PersonalHealth, null) });

        var result = await _sut.GetByPersonIdAsync("198505152345");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.PersonalHealth, insurance.Type);
        Assert.Equal(20m, insurance.MonthlyCost);
    }

    [Fact]
    public async Task GetByPersonIdAsync_EnrichesCarInsurance_WithVehicleData()
    {
        var vehicle = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Car, "ABC123") });
        _vehicleClient.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Car, insurance.Type);
        Assert.Equal(30m, insurance.MonthlyCost);
        Assert.Equal(vehicle, insurance.Vehicle);
    }

    [Fact]
    public async Task GetByPersonIdAsync_CarInsuranceVehicleIsNull_WhenVehicleNotFound()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Car, "ZZZ999") });
        _vehicleClient.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Car, insurance.Type);
        Assert.Null(insurance.Vehicle);
    }

    [Fact]
    public async Task GetByPersonIdAsync_CalculatesTotalMonthlyCost()
    {
        _repository.GetByPersonIdAsync("200203033456", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new PersonInsuranceEntry(InsuranceType.Pet, null),
                new PersonInsuranceEntry(InsuranceType.PersonalHealth, null),
                new PersonInsuranceEntry(InsuranceType.Car, "XYZ789"),
            });
        _vehicleClient.GetByRegistrationAsync("XYZ789", Arg.Any<CancellationToken>())
            .Returns(new VehicleRegistrationModel { RegistrationNumber = "XYZ789" });

        var result = await _sut.GetByPersonIdAsync("200203033456");

        Assert.NotNull(result);
        Assert.Equal(60m, result.TotalMonthlyCost);
    }
}
