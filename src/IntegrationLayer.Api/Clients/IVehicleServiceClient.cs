using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IVehicleServiceClient
{
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default);
}
