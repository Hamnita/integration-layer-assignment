using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private readonly HttpClient _httpClient;

    public InsuranceRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<InsuranceModel>($"insurance/{id}", cancellationToken);
    }

    public async Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<InsuranceModel>>("insurance", cancellationToken)
               ?? Enumerable.Empty<InsuranceModel>();
    }
}
