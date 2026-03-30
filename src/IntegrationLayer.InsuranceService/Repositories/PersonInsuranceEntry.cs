using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public record PersonInsuranceEntry(InsuranceType Type, string? RegistrationNumber);
