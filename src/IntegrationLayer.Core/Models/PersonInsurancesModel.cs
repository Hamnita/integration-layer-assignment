namespace IntegrationLayer.Core.Models;

public class PersonInsurancesModel
{
    public string PersonalIdentificationNumber { get; set; } = string.Empty;
    public IEnumerable<InsuranceSummaryModel> Insurances { get; set; } = [];
    public decimal TotalMonthlyCost { get; set; }
}
