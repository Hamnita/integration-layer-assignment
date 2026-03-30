using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Services;

public interface IVehicleService
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
}
