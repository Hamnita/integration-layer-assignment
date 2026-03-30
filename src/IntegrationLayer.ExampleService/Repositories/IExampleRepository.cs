using IntegrationLayer.Core.Models;

namespace IntegrationLayer.ExampleService.Repositories;

public interface IExampleRepository
{
    Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
