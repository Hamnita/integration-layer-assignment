using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IInsuranceServiceClient
{
    Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}

