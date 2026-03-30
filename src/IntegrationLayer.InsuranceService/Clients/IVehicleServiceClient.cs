using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Clients;

public interface IVehicleServiceClient
{
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default);
}
