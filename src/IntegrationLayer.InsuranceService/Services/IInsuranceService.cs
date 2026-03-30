using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Services;

public interface IInsuranceService
{
    Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
