using IntegrationLayer.Core.Models;
using IntegrationLayer.VehicleService.Repositories;

namespace IntegrationLayer.VehicleService.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => _repository.GetByRegistrationAsync(registrationNumber, cancellationToken);
}
