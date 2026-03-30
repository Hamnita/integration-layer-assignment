using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;

namespace IntegrationLayer.InsuranceService.Services;

public class InsuranceService : IInsuranceService
{
    private static readonly Dictionary<InsuranceType, decimal> _costs = new()
    {
        [InsuranceType.Pet] = 10m,
        [InsuranceType.PersonalHealth] = 20m,
        [InsuranceType.Car] = 30m,
    };

    private readonly IInsuranceRepository _repository;
    private readonly IVehicleServiceClient _vehicleClient;

    public InsuranceService(IInsuranceRepository repository, IVehicleServiceClient vehicleClient)
    {
        _repository = repository;
        _vehicleClient = vehicleClient;
    }

    public Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public async Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetByPersonIdAsync(personId, cancellationToken);
        if (entries is null) return null;

        var insurances = new List<InsuranceSummaryModel>();
        foreach (var entry in entries)
        {
            var summary = new InsuranceSummaryModel
            {
                Type = entry.Type,
                MonthlyCost = _costs[entry.Type],
            };

            if (entry.Type == InsuranceType.Car && entry.RegistrationNumber is not null)
                summary.Vehicle = await _vehicleClient.GetByRegistrationAsync(entry.RegistrationNumber, cancellationToken);

            insurances.Add(summary);
        }

        return new PersonInsurancesModel
        {
            PersonalIdentificationNumber = personId,
            Insurances = insurances,
            TotalMonthlyCost = insurances.Sum(i => i.MonthlyCost),
        };
    }
}
