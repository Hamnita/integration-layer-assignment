using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IExampleServiceClient
{
    Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
