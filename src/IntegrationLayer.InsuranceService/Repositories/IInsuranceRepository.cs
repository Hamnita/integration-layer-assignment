using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public interface IInsuranceRepository
{
    Task<IEnumerable<PersonInsuranceEntry>?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}
