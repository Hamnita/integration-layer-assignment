using IntegrationLayer.Core.Interfaces.Repositories;
using IntegrationLayer.Core.Interfaces.Services;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Services;

public class ExampleService : IExampleService
{
    private readonly IExampleRepository _repository;

    public ExampleService(IExampleRepository repository)
    {
        _repository = repository;
    }

    public Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);
}
