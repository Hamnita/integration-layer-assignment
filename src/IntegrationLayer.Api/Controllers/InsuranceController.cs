using IntegrationLayer.Api.Clients;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsuranceController : ControllerBase
{
    private readonly IInsuranceServiceClient _client;

    public InsuranceController(IInsuranceServiceClient client)
    {
        _client = client;
    }

    [HttpGet("person/{personId}")]
    public async Task<IActionResult> GetByPersonId(string personId, CancellationToken cancellationToken)
    {
        var result = await _client.GetByPersonIdAsync(personId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
