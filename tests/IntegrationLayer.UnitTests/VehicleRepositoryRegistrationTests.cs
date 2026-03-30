using IntegrationLayer.VehicleService.Repositories;

namespace IntegrationLayer.UnitTests;

public class VehicleRepositoryRegistrationTests
{
    private readonly VehicleRepository _sut = new();

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsVehicle_WhenRegistrationExists()
    {
        var result = await _sut.GetByRegistrationAsync("ABC123");

        Assert.NotNull(result);
        Assert.Equal("ABC123", result.RegistrationNumber);
        Assert.Equal("Volvo", result.Make);
        Assert.Equal("XC60", result.Model);
        Assert.Equal(2021, result.Year);
        Assert.Equal("Black", result.Color);
    }

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsNull_WhenRegistrationNotFound()
    {
        var result = await _sut.GetByRegistrationAsync("ZZZ999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_IsCaseInsensitive()
    {
        var result = await _sut.GetByRegistrationAsync("abc123");

        Assert.NotNull(result);
        Assert.Equal("ABC123", result.RegistrationNumber);
    }
}
