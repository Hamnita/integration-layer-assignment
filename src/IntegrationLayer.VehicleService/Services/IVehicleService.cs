using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Services;

public interface IVehicleService
{
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default);
}
