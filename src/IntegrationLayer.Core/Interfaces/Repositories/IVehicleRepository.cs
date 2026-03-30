using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Core.Interfaces.Repositories;

public interface IVehicleRepository
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
