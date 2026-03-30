# Swedish Vehicle Registration Lookup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `GET /api/vehicle/registration/{regNr}` endpoint across the VehicleService microservice and API gateway that returns make, model, year, and color from an in-memory mock.

**Architecture:** The feature follows the existing layered pattern: API gateway `VehicleController` → `VehicleServiceClient` → VehicleService `VehicleController` → `VehicleService` → `VehicleRepository` (in-memory mock). Format validation (`^[A-Za-z]{3}[0-9]{3}$`) and uppercase normalization happen in the VehicleService controller before the service layer is called.

**Tech Stack:** .NET 8, ASP.NET Core, xunit, NSubstitute

---

## File Map

| Action | File |
|--------|------|
| Create | `src/IntegrationLayer.Core/Models/VehicleRegistrationModel.cs` |
| Modify | `src/IntegrationLayer.VehicleService/Repositories/IVehicleRepository.cs` |
| Modify | `src/IntegrationLayer.VehicleService/Repositories/VehicleRepository.cs` |
| Modify | `src/IntegrationLayer.VehicleService/Services/IVehicleService.cs` |
| Modify | `src/IntegrationLayer.VehicleService/Services/VehicleService.cs` |
| Modify | `src/IntegrationLayer.VehicleService/Controllers/VehicleController.cs` |
| Modify | `src/IntegrationLayer.Api/Clients/IVehicleServiceClient.cs` |
| Modify | `src/IntegrationLayer.Api/Clients/VehicleServiceClient.cs` |
| Modify | `src/IntegrationLayer.Api/Controllers/VehicleController.cs` |
| Modify | `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj` |
| Create | `tests/IntegrationLayer.UnitTests/VehicleRepositoryRegistrationTests.cs` |
| Modify | `tests/IntegrationLayer.UnitTests/VehicleServiceTests.cs` |
| Create | `tests/IntegrationLayer.UnitTests/VehicleControllerRegistrationTests.cs` |
| Create | `tests/IntegrationLayer.UnitTests/GatewayVehicleControllerRegistrationTests.cs` |

---

### Task 1: Add VehicleRegistrationModel to Core

**Files:**
- Create: `src/IntegrationLayer.Core/Models/VehicleRegistrationModel.cs`

- [ ] **Step 1: Create the model**

Create `src/IntegrationLayer.Core/Models/VehicleRegistrationModel.cs`:

```csharp
namespace IntegrationLayer.Core.Models;

public class VehicleRegistrationModel
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Verify it builds**

```bash
dotnet build IntegrationLayer.sln
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/IntegrationLayer.Core/Models/VehicleRegistrationModel.cs
git commit -m "feat: add VehicleRegistrationModel to Core"
```

---

### Task 2: Implement GetByRegistrationAsync in VehicleRepository

**Files:**
- Modify: `src/IntegrationLayer.VehicleService/Repositories/IVehicleRepository.cs`
- Modify: `src/IntegrationLayer.VehicleService/Repositories/VehicleRepository.cs`
- Create: `tests/IntegrationLayer.UnitTests/VehicleRepositoryRegistrationTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/IntegrationLayer.UnitTests/VehicleRepositoryRegistrationTests.cs`:

```csharp
using IntegrationLayer.VehicleService.Repositories;

namespace IntegrationLayer.UnitTests;

public class VehicleRepositoryRegistrationTests
{
    // GetByRegistrationAsync uses only in-memory data — no HTTP calls made,
    // so we can construct the repository with a bare HttpClient.
    private readonly VehicleRepository _sut = new(new HttpClient());

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsVehicle_WhenRegistrationExists()
    {
        var result = await _sut.GetByRegistrationAsync("ABC123");

        Assert.NotNull(result);
        Assert.Equal("ABC123", result.RegistrationNumber);
        Assert.Equal("Volvo", result.Make);
        Assert.Equal("XC60", result.Model);
        Assert.Equal(2021, result.Year);
        Assert.Equal("Black", result.Color);
    }

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsNull_WhenRegistrationNotFound()
    {
        var result = await _sut.GetByRegistrationAsync("ZZZ999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_IsCaseInsensitive()
    {
        var result = await _sut.GetByRegistrationAsync("abc123");

        Assert.NotNull(result);
        Assert.Equal("ABC123", result.RegistrationNumber);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleRepositoryRegistrationTests"
```

Expected: compile error — `GetByRegistrationAsync` does not exist yet.

- [ ] **Step 3: Add GetByRegistrationAsync to IVehicleRepository**

Replace the contents of `src/IntegrationLayer.VehicleService/Repositories/IVehicleRepository.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Repositories;

public interface IVehicleRepository
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement GetByRegistrationAsync in VehicleRepository**

Replace the contents of `src/IntegrationLayer.VehicleService/Repositories/VehicleRepository.cs`:

```csharp
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, VehicleRegistrationModel> _registrationMocks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ABC123"] = new() { RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black" },
            ["XYZ789"] = new() { RegistrationNumber = "XYZ789", Make = "Saab", Model = "9-3", Year = 2008, Color = "Silver" },
            ["DEF456"] = new() { RegistrationNumber = "DEF456", Make = "Volvo", Model = "V70", Year = 2015, Color = "White" },
        };

    public VehicleRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleModel>($"vehicles/{id}", cancellationToken);
    }

    public async Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<VehicleModel>>("vehicles", cancellationToken)
               ?? Enumerable.Empty<VehicleModel>();
    }

    public Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_registrationMocks.GetValueOrDefault(registrationNumber));
}
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleRepositoryRegistrationTests"
```

Expected: 3 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/IntegrationLayer.VehicleService/Repositories/IVehicleRepository.cs \
        src/IntegrationLayer.VehicleService/Repositories/VehicleRepository.cs \
        tests/IntegrationLayer.UnitTests/VehicleRepositoryRegistrationTests.cs
git commit -m "feat: implement GetByRegistrationAsync in VehicleRepository with in-memory mock"
```

---

### Task 3: Add GetByRegistrationAsync to IVehicleService and VehicleService

**Files:**
- Modify: `src/IntegrationLayer.VehicleService/Services/IVehicleService.cs`
- Modify: `src/IntegrationLayer.VehicleService/Services/VehicleService.cs`
- Modify: `tests/IntegrationLayer.UnitTests/VehicleServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Append these three test methods to the `VehicleServiceTests` class in `tests/IntegrationLayer.UnitTests/VehicleServiceTests.cs` (inside the existing class, after the last `}`  before the class-closing `}`):

```csharp
    [Fact]
    public async Task GetByRegistrationAsync_DelegatesToRepository()
    {
        var expected = new VehicleRegistrationModel { RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black" };
        _repository.GetByRegistrationAsync("ABC123").Returns(expected);

        var result = await _sut.GetByRegistrationAsync("ABC123");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByRegistrationAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        _repository.GetByRegistrationAsync("ZZZ999").Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistrationAsync("ZZZ999");

        Assert.Null(result);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleServiceTests"
```

Expected: compile error — `GetByRegistrationAsync` does not exist on `IVehicleService`.

- [ ] **Step 3: Add GetByRegistrationAsync to IVehicleService**

Replace the contents of `src/IntegrationLayer.VehicleService/Services/IVehicleService.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.VehicleService.Services;

public interface IVehicleService
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement GetByRegistrationAsync in VehicleService**

Replace the contents of `src/IntegrationLayer.VehicleService/Services/VehicleService.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.VehicleService.Repositories;

namespace IntegrationLayer.VehicleService.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<VehicleRegistrationModel?> GetByRegistrationAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => _repository.GetByRegistrationAsync(registrationNumber, cancellationToken);
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleServiceTests"
```

Expected: 5 tests pass (3 existing + 2 new).

- [ ] **Step 6: Commit**

```bash
git add src/IntegrationLayer.VehicleService/Services/IVehicleService.cs \
        src/IntegrationLayer.VehicleService/Services/VehicleService.cs \
        tests/IntegrationLayer.UnitTests/VehicleServiceTests.cs
git commit -m "feat: add GetByRegistrationAsync to IVehicleService and VehicleService"
```

---

### Task 4: Add registration endpoint to VehicleService VehicleController

**Files:**
- Modify: `src/IntegrationLayer.VehicleService/Controllers/VehicleController.cs`
- Create: `tests/IntegrationLayer.UnitTests/VehicleControllerRegistrationTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/IntegrationLayer.UnitTests/VehicleControllerRegistrationTests.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.VehicleService.Controllers;
using IntegrationLayer.VehicleService.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class VehicleControllerRegistrationTests
{
    private readonly IVehicleService _service = Substitute.For<IVehicleService>();
    private readonly VehicleController _sut;

    public VehicleControllerRegistrationTests()
    {
        _sut = new VehicleController(_service);
    }

    [Theory]
    [InlineData("AB1234")]   // too many digits
    [InlineData("ABCD12")]   // too many letters
    [InlineData("123ABC")]   // wrong order
    [InlineData("AB")]       // too short
    [InlineData("ABC12")]    // only 2 digits
    [InlineData("ABC1234")]  // 4 digits
    public async Task GetByRegistration_ReturnsBadRequest_WhenFormatInvalid(string regNr)
    {
        var result = await _sut.GetByRegistration(regNr, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsNotFound_WhenNotInMock()
    {
        _service.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistration("ZZZ999", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsOk_WithModel_WhenFound()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _service.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("ABC123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByRegistration_NormalizesInputToUppercase_BeforeCallingService()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _service.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("abc123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
        await _service.Received(1).GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleControllerRegistrationTests"
```

Expected: compile error — `GetByRegistration` action does not exist yet.

- [ ] **Step 3: Implement the endpoint in VehicleService VehicleController**

Replace the contents of `src/IntegrationLayer.VehicleService/Controllers/VehicleController.cs`:

```csharp
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
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "VehicleControllerRegistrationTests"
```

Expected: 9 tests pass (6 bad-request cases + NotFound + Ok + normalization).

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.VehicleService/Controllers/VehicleController.cs \
        tests/IntegrationLayer.UnitTests/VehicleControllerRegistrationTests.cs
git commit -m "feat: add registration endpoint to VehicleService controller"
```

---

### Task 5: Add GetByRegistrationAsync to IVehicleServiceClient and VehicleServiceClient

**Files:**
- Modify: `src/IntegrationLayer.Api/Clients/IVehicleServiceClient.cs`
- Modify: `src/IntegrationLayer.Api/Clients/VehicleServiceClient.cs`

- [ ] **Step 1: Add method to IVehicleServiceClient**

Replace the contents of `src/IntegrationLayer.Api/Clients/IVehicleServiceClient.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IVehicleServiceClient
{
    Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implement in VehicleServiceClient**

Replace the contents of `src/IntegrationLayer.Api/Clients/VehicleServiceClient.cs`:

```csharp
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public class VehicleServiceClient : IVehicleServiceClient
{
    private readonly HttpClient _httpClient;

    public VehicleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleModel>($"api/vehicle/{id}", cancellationToken);
    }

    public async Task<IEnumerable<VehicleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<VehicleModel>>("api/vehicle", cancellationToken)
               ?? Enumerable.Empty<VehicleModel>();
    }

    public async Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleRegistrationModel>($"api/vehicle/registration/{regNr}", cancellationToken);
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build IntegrationLayer.sln
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/IntegrationLayer.Api/Clients/IVehicleServiceClient.cs \
        src/IntegrationLayer.Api/Clients/VehicleServiceClient.cs
git commit -m "feat: add GetByRegistrationAsync to VehicleServiceClient"
```

---

### Task 6: Add registration endpoint to API gateway VehicleController

**Files:**
- Modify: `src/IntegrationLayer.Api/Controllers/VehicleController.cs`
- Modify: `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj`
- Create: `tests/IntegrationLayer.UnitTests/GatewayVehicleControllerRegistrationTests.cs`

- [ ] **Step 1: Add IntegrationLayer.Api project reference to UnitTests**

Open `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj` and add the following line inside the existing `<ItemGroup>` that contains project references:

```xml
<ProjectReference Include="..\..\src\IntegrationLayer.Api\IntegrationLayer.Api.csproj" />
```

The `<ItemGroup>` block should look like:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\IntegrationLayer.Core\IntegrationLayer.Core.csproj" />
    <ProjectReference Include="..\..\src\IntegrationLayer.VehicleService\IntegrationLayer.VehicleService.csproj" />
    <ProjectReference Include="..\..\src\IntegrationLayer.Api\IntegrationLayer.Api.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Write the failing tests**

Create `tests/IntegrationLayer.UnitTests/GatewayVehicleControllerRegistrationTests.cs`:

```csharp
using IntegrationLayer.Api.Clients;
using IntegrationLayer.Api.Controllers;
using IntegrationLayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class GatewayVehicleControllerRegistrationTests
{
    private readonly IVehicleServiceClient _client = Substitute.For<IVehicleServiceClient>();
    private readonly VehicleController _sut;

    public GatewayVehicleControllerRegistrationTests()
    {
        _sut = new VehicleController(_client);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsOk_WithModelFromClient()
    {
        var model = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _client.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByRegistration("ABC123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByRegistration_ReturnsNotFound_WhenClientReturnsNull()
    {
        _client.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByRegistration("ZZZ999", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "GatewayVehicleControllerRegistrationTests"
```

Expected: compile error — `GetByRegistration` action does not exist on the gateway controller yet.

- [ ] **Step 4: Implement the endpoint in the API gateway VehicleController**

Replace the contents of `src/IntegrationLayer.Api/Controllers/VehicleController.cs`:

```csharp
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

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var results = await _client.GetAllAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _client.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("registration/{regNr}")]
    public async Task<IActionResult> GetByRegistration(string regNr, CancellationToken cancellationToken)
    {
        var result = await _client.GetByRegistrationAsync(regNr, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

- [ ] **Step 5: Run all unit tests**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj
```

Expected: all tests pass (existing + all new tests).

- [ ] **Step 6: Commit**

```bash
git add src/IntegrationLayer.Api/Controllers/VehicleController.cs \
        tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj \
        tests/IntegrationLayer.UnitTests/GatewayVehicleControllerRegistrationTests.cs
git commit -m "feat: add registration endpoint to API gateway VehicleController"
```

---

## Manual Smoke Test

Once all services are running (`dotnet run` in both `IntegrationLayer.VehicleService` and `IntegrationLayer.Api`):

```bash
# Returns 200 with vehicle data
curl http://localhost:5100/api/vehicle/registration/ABC123

# Returns 200 (case-insensitive input normalised by VehicleService)
curl http://localhost:5100/api/vehicle/registration/abc123

# Returns 404
curl http://localhost:5100/api/vehicle/registration/ZZZ999

# Returns 400 (bad format)
curl http://localhost:5100/api/vehicle/registration/INVALID
```

Expected response for `ABC123`:
```json
{
  "registrationNumber": "ABC123",
  "make": "Volvo",
  "model": "XC60",
  "year": 2021,
  "color": "Black"
}
```
