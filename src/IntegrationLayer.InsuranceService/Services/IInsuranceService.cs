using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Services;

public interface IInsuranceService
{
    Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}
