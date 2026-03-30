using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Repositories;

namespace IntegrationLayer.InsuranceService.Services;

public class InsuranceService : IInsuranceService
{
    private readonly IInsuranceRepository _repository;

    public InsuranceService(IInsuranceRepository repository)
    {
        _repository = repository;
    }

    public Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);
}
