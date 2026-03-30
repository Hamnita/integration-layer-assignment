using System.Text.RegularExpressions;
using IntegrationLayer.VehicleService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.VehicleService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private static readonly Regex RegistrationRegex = new(@"^[A-Za-z]{3}[0-9]{3}$", RegexOptions.Compiled);

    private readonly IVehicleService _service;

    public VehicleController(IVehicleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var results = await _service.GetAllAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("registration/{regNr}")]
    public async Task<IActionResult> GetByRegistration(string regNr, CancellationToken cancellationToken)
    {
        if (!RegistrationRegex.IsMatch(regNr))
            return BadRequest("Invalid registration number format. Expected 3 letters followed by 3 digits (e.g. ABC123).");

        var normalized = regNr.ToUpperInvariant();
        var result = await _service.GetByRegistrationAsync(normalized, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
