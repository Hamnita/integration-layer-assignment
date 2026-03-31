using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Clients;

public class VehicleServiceClient : IVehicleServiceClient
{
    private readonly HttpClient _httpClient;

    public VehicleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/vehicle/registration/{regNr}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VehicleRegistrationModel>(cancellationToken);
    }
}
