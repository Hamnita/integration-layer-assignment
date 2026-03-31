using System.Text.RegularExpressions;
using IntegrationLayer.Api.Clients;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private static readonly Regex RegistrationRegex = new(@"^[A-Za-z]{3}[0-9]{3}$", RegexOptions.Compiled);

    private readonly IVehicleServiceClient _client;

    public VehicleController(IVehicleServiceClient client)
    {
        _client = client;
    }

    [HttpGet("registration/{regNr}")]
    public async Task<IActionResult> GetByRegistration(string regNr, CancellationToken cancellationToken)
    {
        if (!RegistrationRegex.IsMatch(regNr))
            return BadRequest("Invalid registration number format. Expected 3 letters followed by 3 digits (e.g. ABC123).");

        var result = await _client.GetByRegistrationAsync(regNr, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
