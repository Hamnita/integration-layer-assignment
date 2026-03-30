using IntegrationLayer.Core.Models;
using IntegrationLayer.VehicleService.Repositories;
using IntegrationLayer.VehicleService.Services;
using NSubstitute;
using VehicleServiceImpl = IntegrationLayer.VehicleService.Services.VehicleService;

namespace IntegrationLayer.UnitTests;

public class VehicleServiceTests
{
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();
    private readonly VehicleServiceImpl _sut;

    public VehicleServiceTests()
    {
        _sut = new VehicleServiceImpl(_repository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        var expected = new[] { new VehicleModel { Id = 1, Name = "Test" } };
        _repository.GetAllAsync().Returns(expected);

        var result = await _sut.GetAllAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenFound()
    {
        var expected = new VehicleModel { Id = 1, Name = "Test" };
        _repository.GetByIdAsync(1).Returns(expected);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetByIdAsync(99).Returns((VehicleModel?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_DelegatesToRepository()
    {
        var expected = new VehicleRegistrationModel { RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black" };
        _repository.GetByRegistrationAsync("ABC123").Returns(expected);

        var result = await _sut.GetByRegistrationAsync("ABC123");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        _repository.GetByRegistrationAsync("ZZZ999").Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistrationAsync("ZZZ999");

        Assert.Null(result);
    }
}
