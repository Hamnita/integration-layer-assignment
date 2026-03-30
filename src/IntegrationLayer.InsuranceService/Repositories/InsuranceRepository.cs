using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private static readonly Dictionary<string, List<PersonInsuranceEntry>> _personInsuranceMocks =
        new()
        {
            ["199001011234"] =
            [
                new(InsuranceType.Pet, null),
                new(InsuranceType.Car, "ABC123"),
            ],
            ["198505152345"] =
            [
                new(InsuranceType.PersonalHealth, null),
            ],
            ["200203033456"] =
            [
                new(InsuranceType.Pet, null),
                new(InsuranceType.PersonalHealth, null),
                new(InsuranceType.Car, "XYZ789"),
            ],
        };

    public Task<IEnumerable<PersonInsuranceEntry>?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        _personInsuranceMocks.TryGetValue(personId, out var result);
        return Task.FromResult<IEnumerable<PersonInsuranceEntry>?>(result);
    }
}
