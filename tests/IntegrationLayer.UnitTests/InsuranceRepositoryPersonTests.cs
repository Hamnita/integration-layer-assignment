using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Repositories;

namespace IntegrationLayer.UnitTests;

public class InsuranceRepositoryPersonTests
{
    // GetByPersonIdAsync uses only in-memory data — no HTTP calls made
    private readonly InsuranceRepository _sut = new(new HttpClient());

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsEntries_WhenPersonExists()
    {
        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var entries = result.ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Type == InsuranceType.Pet);
        Assert.Contains(entries, e => e.Type == InsuranceType.Car && e.RegistrationNumber == "ABC123");
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsNull_WhenPersonNotFound()
    {
        var result = await _sut.GetByPersonIdAsync("000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsAllInsurances_ForPersonWithThree()
    {
        var result = await _sut.GetByPersonIdAsync("200203033456");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }
}
