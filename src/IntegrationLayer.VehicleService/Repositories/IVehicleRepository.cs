using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Repositories;

public interface IVehicleRepository
{
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default);
}
