using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public class ExampleServiceClient : IExampleServiceClient
{
    private readonly HttpClient _httpClient;

    public ExampleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExampleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ExampleModel>($"api/example/{id}", cancellationToken);
    }

    public async Task<IEnumerable<ExampleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ExampleModel>>("api/example", cancellationToken)
               ?? Enumerable.Empty<ExampleModel>();
    }
}
