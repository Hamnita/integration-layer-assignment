using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IVehicleServiceClient
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
