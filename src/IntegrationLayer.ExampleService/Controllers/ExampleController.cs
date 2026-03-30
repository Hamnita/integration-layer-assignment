using IntegrationLayer.ExampleService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationLayer.ExampleService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _service;

    public ExampleController(IExampleService service)
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
}
