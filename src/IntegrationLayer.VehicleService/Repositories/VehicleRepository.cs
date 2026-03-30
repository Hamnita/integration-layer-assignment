using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, VehicleRegistrationModel> _registrationMocks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ABC123"] = new() { RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black" },
            ["XYZ789"] = new() { RegistrationNumber = "XYZ789", Make = "Saab", Model = "9-3", Year = 2008, Color = "Silver" },
            ["DEF456"] = new() { RegistrationNumber = "DEF456", Make = "Volvo", Model = "V70", Year = 2015, Color = "White" },
        };

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

    public Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_registrationMocks.GetValueOrDefault(registrationNumber));
}
