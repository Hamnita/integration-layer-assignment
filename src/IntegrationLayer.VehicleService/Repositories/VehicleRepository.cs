using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly HttpClient _httpClient;

    public VehicleRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleModel>($"vehicles/{id}", cancellationToken);
    }

    public async Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<VehicleModel>>("vehicles", cancellationToken)
               ?? Enumerable.Empty<VehicleModel>();
    }
}
