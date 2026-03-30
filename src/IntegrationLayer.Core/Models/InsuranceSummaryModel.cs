using System.Text.Json.Serialization;

namespace IntegrationLayer.Core.Models;

public class InsuranceSummaryModel
{
    public InsuranceType Type { get; set; }
    public decimal MonthlyCost { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VehicleRegistrationModel? Vehicle { get; set; }
}
