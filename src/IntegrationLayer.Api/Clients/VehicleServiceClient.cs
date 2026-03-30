using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public class VehicleServiceClient : IVehicleServiceClient
{
    private readonly HttpClient _httpClient;

    public VehicleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleRegistrationModel>($"api/vehicle/registration/{regNr}", cancellationToken);
    }
}
