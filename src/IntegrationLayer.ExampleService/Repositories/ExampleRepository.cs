using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.ExampleService.Repositories;

public class ExampleRepository : IExampleRepository
{
    private readonly HttpClient _httpClient;

    public ExampleRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ExampleModel>($"examples/{id}", cancellationToken);
    }

    public async Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ExampleModel>>("examples", cancellationToken)
               ?? Enumerable.Empty<ExampleModel>();
    }
}
