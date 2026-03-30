namespace IntegrationLayer.Core.Models;

public class InsuranceSummaryModel
{
    public InsuranceType Type { get; set; }
    public decimal MonthlyCost { get; set; }
    public VehicleRegistrationModel? Vehicle { get; set; }
}
