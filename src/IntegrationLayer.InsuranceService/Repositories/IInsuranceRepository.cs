using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public interface IInsuranceRepository
{
    Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
