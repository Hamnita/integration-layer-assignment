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

    public async Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/insurance/person/{personId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonInsurancesModel>(cancellationToken);
    }
}
