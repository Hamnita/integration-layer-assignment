namespace IntegrationLayer.Core.Models;

public class InsuranceModel
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}
