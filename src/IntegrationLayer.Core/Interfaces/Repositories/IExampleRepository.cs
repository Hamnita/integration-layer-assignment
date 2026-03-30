using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Core.Interfaces.Repositories;

public interface IExampleRepository
{
    Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
