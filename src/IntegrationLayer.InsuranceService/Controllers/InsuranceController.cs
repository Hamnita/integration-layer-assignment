using System.Text.RegularExpressions;
using IntegrationLayer.InsuranceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.InsuranceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsuranceController : ControllerBase
{
    private static readonly Regex PersonIdRegex = new(@"^\d{8}-?\d{4}$", RegexOptions.Compiled);

    private readonly IInsuranceService _service;

    public InsuranceController(IInsuranceService service)
    {
        _service = service;
    }

    [HttpGet("person/{personId}")]
    public async Task<IActionResult> GetByPersonId(string personId, CancellationToken cancellationToken)
    {
        if (!PersonIdRegex.IsMatch(personId))
            return BadRequest();

        var normalizedId = personId.Replace("-", "");
        var result = await _service.GetByPersonIdAsync(normalizedId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
