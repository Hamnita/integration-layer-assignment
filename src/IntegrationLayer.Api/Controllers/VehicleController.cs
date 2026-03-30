using IntegrationLayer.Api.Clients;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleServiceClient _client;

    public VehicleController(IVehicleServiceClient client)
    {
        _client = client;
    }

    [HttpGet("registration/{regNr}")]
    public async Task<IActionResult> GetByRegistration(string regNr, CancellationToken cancellationToken)
    {
        var result = await _client.GetByRegistrationAsync(regNr, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
