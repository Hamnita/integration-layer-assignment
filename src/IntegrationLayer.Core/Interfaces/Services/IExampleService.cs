using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Core.Interfaces.Services;

public interface IExampleService
{
    Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
