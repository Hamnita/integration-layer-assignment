using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Core.Interfaces.Services;

public interface IVehicleService
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
