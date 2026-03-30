using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public class InsuranceServiceClient : IInsuranceServiceClient
{
    private readonly HttpClient _httpClient;

    public InsuranceServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<InsuranceModel>($"api/insurance/{id}", cancellationToken);
    }

    public async Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<InsuranceModel>>("api/insurance", cancellationToken)
               ?? Enumerable.Empty<InsuranceModel>();
    }
}
