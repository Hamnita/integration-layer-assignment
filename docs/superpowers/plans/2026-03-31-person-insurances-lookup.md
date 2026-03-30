# Person Insurances Lookup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `GET /api/insurance/person/{personId}` that returns all insurances for a Swedish personnummer, with monthly costs, and vehicle info for car insurances fetched from VehicleService.

**Architecture:** InsuranceService holds in-memory mock data (personId → insurance entries), assigns fixed monthly costs, and calls VehicleService directly for car insurance enrichment. The API gateway proxies the request through. Validation (12-digit personnummer with optional dash) happens in the InsuranceService controller.

**Tech Stack:** .NET 8, ASP.NET Core, xunit, NSubstitute

---

## File Map

| Action | File |
|--------|------|
| Create | `src/IntegrationLayer.Core/Models/InsuranceType.cs` |
| Create | `src/IntegrationLayer.Core/Models/InsuranceSummaryModel.cs` |
| Create | `src/IntegrationLayer.Core/Models/PersonInsurancesModel.cs` |
| Create | `src/IntegrationLayer.InsuranceService/Clients/IVehicleServiceClient.cs` |
| Create | `src/IntegrationLayer.InsuranceService/Clients/VehicleServiceClient.cs` |
| Create | `src/IntegrationLayer.InsuranceService/Repositories/PersonInsuranceEntry.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Repositories/IInsuranceRepository.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Repositories/InsuranceRepository.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Services/IInsuranceService.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Services/InsuranceService.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Controllers/InsuranceController.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/Program.cs` |
| Modify | `src/IntegrationLayer.InsuranceService/appsettings.Development.json` |
| Modify | `src/IntegrationLayer.Api/Clients/IInsuranceServiceClient.cs` |
| Modify | `src/IntegrationLayer.Api/Clients/InsuranceServiceClient.cs` |
| Modify | `src/IntegrationLayer.Api/Controllers/InsuranceController.cs` |
| Modify | `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj` |
| Create | `tests/IntegrationLayer.UnitTests/InsuranceRepositoryPersonTests.cs` |
| Create | `tests/IntegrationLayer.UnitTests/InsuranceServicePersonTests.cs` |
| Create | `tests/IntegrationLayer.UnitTests/InsuranceControllerPersonTests.cs` |
| Create | `tests/IntegrationLayer.UnitTests/GatewayInsuranceControllerPersonTests.cs` |

---

### Task 1: Add Core models

**Files:**
- Create: `src/IntegrationLayer.Core/Models/InsuranceType.cs`
- Create: `src/IntegrationLayer.Core/Models/InsuranceSummaryModel.cs`
- Create: `src/IntegrationLayer.Core/Models/PersonInsurancesModel.cs`

- [ ] **Step 1: Create InsuranceType enum**

Create `src/IntegrationLayer.Core/Models/InsuranceType.cs`:

```csharp
namespace IntegrationLayer.Core.Models;

public enum InsuranceType { Pet, PersonalHealth, Car }
```

- [ ] **Step 2: Create InsuranceSummaryModel**

Create `src/IntegrationLayer.Core/Models/InsuranceSummaryModel.cs`:

```csharp
namespace IntegrationLayer.Core.Models;

public class InsuranceSummaryModel
{
    public InsuranceType Type { get; set; }
    public decimal MonthlyCost { get; set; }
    public VehicleRegistrationModel? Vehicle { get; set; }
}
```

- [ ] **Step 3: Create PersonInsurancesModel**

Create `src/IntegrationLayer.Core/Models/PersonInsurancesModel.cs`:

```csharp
namespace IntegrationLayer.Core.Models;

public class PersonInsurancesModel
{
    public string PersonalIdentificationNumber { get; set; } = string.Empty;
    public IEnumerable<InsuranceSummaryModel> Insurances { get; set; } = [];
    public decimal TotalMonthlyCost { get; set; }
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build IntegrationLayer.sln --verbosity quiet
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.Core/Models/InsuranceType.cs \
        src/IntegrationLayer.Core/Models/InsuranceSummaryModel.cs \
        src/IntegrationLayer.Core/Models/PersonInsurancesModel.cs
git commit -m "feat: add InsuranceType, InsuranceSummaryModel, PersonInsurancesModel to Core"
```

---

### Task 2: Add IVehicleServiceClient and VehicleServiceClient to InsuranceService

**Files:**
- Create: `src/IntegrationLayer.InsuranceService/Clients/IVehicleServiceClient.cs`
- Create: `src/IntegrationLayer.InsuranceService/Clients/VehicleServiceClient.cs`

- [ ] **Step 1: Create IVehicleServiceClient interface**

Create `src/IntegrationLayer.InsuranceService/Clients/IVehicleServiceClient.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Clients;

public interface IVehicleServiceClient
{
    Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create VehicleServiceClient implementation**

Create `src/IntegrationLayer.InsuranceService/Clients/VehicleServiceClient.cs`:

```csharp
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Clients;

public class VehicleServiceClient : IVehicleServiceClient
{
    private readonly HttpClient _httpClient;

    public VehicleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleRegistrationModel?> GetByRegistrationAsync(string regNr, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<VehicleRegistrationModel>($"api/vehicle/registration/{regNr}", cancellationToken);
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build IntegrationLayer.sln --verbosity quiet
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/Clients/IVehicleServiceClient.cs \
        src/IntegrationLayer.InsuranceService/Clients/VehicleServiceClient.cs
git commit -m "feat: add IVehicleServiceClient and VehicleServiceClient to InsuranceService"
```

---

### Task 3: Add GetByPersonIdAsync to InsuranceRepository

**Files:**
- Create: `src/IntegrationLayer.InsuranceService/Repositories/PersonInsuranceEntry.cs`
- Modify: `src/IntegrationLayer.InsuranceService/Repositories/IInsuranceRepository.cs`
- Modify: `src/IntegrationLayer.InsuranceService/Repositories/InsuranceRepository.cs`
- Modify: `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj`
- Create: `tests/IntegrationLayer.UnitTests/InsuranceRepositoryPersonTests.cs`

- [ ] **Step 1: Create PersonInsuranceEntry record**

Create `src/IntegrationLayer.InsuranceService/Repositories/PersonInsuranceEntry.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public record PersonInsuranceEntry(InsuranceType Type, string? RegistrationNumber);
```

- [ ] **Step 2: Add InsuranceService project reference to UnitTests**

Open `tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj` and add one line inside the existing project-references `<ItemGroup>`:

```xml
<ProjectReference Include="..\..\src\IntegrationLayer.InsuranceService\IntegrationLayer.InsuranceService.csproj" />
```

The full `<ItemGroup>` block should look like:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\IntegrationLayer.Core\IntegrationLayer.Core.csproj" />
    <ProjectReference Include="..\..\src\IntegrationLayer.VehicleService\IntegrationLayer.VehicleService.csproj" />
    <ProjectReference Include="..\..\src\IntegrationLayer.Api\IntegrationLayer.Api.csproj" />
    <ProjectReference Include="..\..\src\IntegrationLayer.InsuranceService\IntegrationLayer.InsuranceService.csproj" />
  </ItemGroup>
```

- [ ] **Step 3: Write the failing test**

Create `tests/IntegrationLayer.UnitTests/InsuranceRepositoryPersonTests.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Repositories;

namespace IntegrationLayer.UnitTests;

public class InsuranceRepositoryPersonTests
{
    // GetByPersonIdAsync uses only in-memory data — no HTTP calls made
    private readonly InsuranceRepository _sut = new(new HttpClient());

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsEntries_WhenPersonExists()
    {
        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var entries = result.ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Type == InsuranceType.Pet);
        Assert.Contains(entries, e => e.Type == InsuranceType.Car && e.RegistrationNumber == "ABC123");
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsNull_WhenPersonNotFound()
    {
        var result = await _sut.GetByPersonIdAsync("000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsAllInsurances_ForPersonWithThree()
    {
        var result = await _sut.GetByPersonIdAsync("200203033456");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceRepositoryPersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: compile error — `GetByPersonIdAsync` does not exist yet.

- [ ] **Step 5: Add GetByPersonIdAsync to IInsuranceRepository**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Repositories/IInsuranceRepository.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public interface IInsuranceRepository
{
    Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PersonInsuranceEntry>?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 6: Implement GetByPersonIdAsync in InsuranceRepository**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Repositories/InsuranceRepository.cs`:

```csharp
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, List<PersonInsuranceEntry>> _personInsuranceMocks =
        new()
        {
            ["199001011234"] =
            [
                new(InsuranceType.Pet, null),
                new(InsuranceType.Car, "ABC123"),
            ],
            ["198505152345"] =
            [
                new(InsuranceType.PersonalHealth, null),
            ],
            ["200203033456"] =
            [
                new(InsuranceType.Pet, null),
                new(InsuranceType.PersonalHealth, null),
                new(InsuranceType.Car, "XYZ789"),
            ],
        };

    public InsuranceRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<InsuranceModel>($"insurance/{id}", cancellationToken);
    }

    public async Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<InsuranceModel>>("insurance", cancellationToken)
               ?? Enumerable.Empty<InsuranceModel>();
    }

    public Task<IEnumerable<PersonInsuranceEntry>?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        _personInsuranceMocks.TryGetValue(personId, out var result);
        return Task.FromResult<IEnumerable<PersonInsuranceEntry>?>(result);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceRepositoryPersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 8: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/Repositories/PersonInsuranceEntry.cs \
        src/IntegrationLayer.InsuranceService/Repositories/IInsuranceRepository.cs \
        src/IntegrationLayer.InsuranceService/Repositories/InsuranceRepository.cs \
        tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj \
        tests/IntegrationLayer.UnitTests/InsuranceRepositoryPersonTests.cs
git commit -m "feat: implement GetByPersonIdAsync in InsuranceRepository with in-memory mock"
```

---

### Task 4: Add GetByPersonIdAsync to IInsuranceService and InsuranceService

**Files:**
- Modify: `src/IntegrationLayer.InsuranceService/Services/IInsuranceService.cs`
- Modify: `src/IntegrationLayer.InsuranceService/Services/InsuranceService.cs`
- Create: `tests/IntegrationLayer.UnitTests/InsuranceServicePersonTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/IntegrationLayer.UnitTests/InsuranceServicePersonTests.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;
using NSubstitute;
using InsuranceServiceImpl = IntegrationLayer.InsuranceService.Services.InsuranceService;

namespace IntegrationLayer.UnitTests;

public class InsuranceServicePersonTests
{
    private readonly IInsuranceRepository _repository = Substitute.For<IInsuranceRepository>();
    private readonly IVehicleServiceClient _vehicleClient = Substitute.For<IVehicleServiceClient>();
    private readonly InsuranceServiceImpl _sut;

    public InsuranceServicePersonTests()
    {
        _sut = new InsuranceServiceImpl(_repository, _vehicleClient);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsNull_WhenPersonNotFound()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((IEnumerable<PersonInsuranceEntry>?)null);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsPetInsurance_WithCorrectCost()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Pet, null) });

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        Assert.Equal("199001011234", result.PersonalIdentificationNumber);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Pet, insurance.Type);
        Assert.Equal(10m, insurance.MonthlyCost);
        Assert.Null(insurance.Vehicle);
        Assert.Equal(10m, result.TotalMonthlyCost);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsPersonalHealthInsurance_WithCorrectCost()
    {
        _repository.GetByPersonIdAsync("198505152345", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.PersonalHealth, null) });

        var result = await _sut.GetByPersonIdAsync("198505152345");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.PersonalHealth, insurance.Type);
        Assert.Equal(20m, insurance.MonthlyCost);
    }

    [Fact]
    public async Task GetByPersonIdAsync_EnrichesCarInsurance_WithVehicleData()
    {
        var vehicle = new VehicleRegistrationModel
        {
            RegistrationNumber = "ABC123", Make = "Volvo", Model = "XC60", Year = 2021, Color = "Black"
        };
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Car, "ABC123") });
        _vehicleClient.GetByRegistrationAsync("ABC123", Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Car, insurance.Type);
        Assert.Equal(30m, insurance.MonthlyCost);
        Assert.Equal(vehicle, insurance.Vehicle);
    }

    [Fact]
    public async Task GetByPersonIdAsync_CarInsuranceVehicleIsNull_WhenVehicleNotFound()
    {
        _repository.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns(new[] { new PersonInsuranceEntry(InsuranceType.Car, "ZZZ999") });
        _vehicleClient.GetByRegistrationAsync("ZZZ999", Arg.Any<CancellationToken>())
            .Returns((VehicleRegistrationModel?)null);

        var result = await _sut.GetByPersonIdAsync("199001011234");

        Assert.NotNull(result);
        var insurance = Assert.Single(result.Insurances);
        Assert.Equal(InsuranceType.Car, insurance.Type);
        Assert.Null(insurance.Vehicle);
    }

    [Fact]
    public async Task GetByPersonIdAsync_CalculatesTotalMonthlyCost()
    {
        _repository.GetByPersonIdAsync("200203033456", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new PersonInsuranceEntry(InsuranceType.Pet, null),
                new PersonInsuranceEntry(InsuranceType.PersonalHealth, null),
                new PersonInsuranceEntry(InsuranceType.Car, "XYZ789"),
            });
        _vehicleClient.GetByRegistrationAsync("XYZ789", Arg.Any<CancellationToken>())
            .Returns(new VehicleRegistrationModel { RegistrationNumber = "XYZ789" });

        var result = await _sut.GetByPersonIdAsync("200203033456");

        Assert.NotNull(result);
        Assert.Equal(60m, result.TotalMonthlyCost); // 10 + 20 + 30
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceServicePersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: compile error — `GetByPersonIdAsync` does not exist on `IInsuranceService`.

- [ ] **Step 3: Add GetByPersonIdAsync to IInsuranceService**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Services/IInsuranceService.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.InsuranceService.Services;

public interface IInsuranceService
{
    Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement GetByPersonIdAsync in InsuranceService**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Services/InsuranceService.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;

namespace IntegrationLayer.InsuranceService.Services;

public class InsuranceService : IInsuranceService
{
    private static readonly Dictionary<InsuranceType, decimal> _costs = new()
    {
        [InsuranceType.Pet] = 10m,
        [InsuranceType.PersonalHealth] = 20m,
        [InsuranceType.Car] = 30m,
    };

    private readonly IInsuranceRepository _repository;
    private readonly IVehicleServiceClient _vehicleClient;

    public InsuranceService(IInsuranceRepository repository, IVehicleServiceClient vehicleClient)
    {
        _repository = repository;
        _vehicleClient = vehicleClient;
    }

    public Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public async Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetByPersonIdAsync(personId, cancellationToken);
        if (entries is null) return null;

        var insurances = new List<InsuranceSummaryModel>();
        foreach (var entry in entries)
        {
            var summary = new InsuranceSummaryModel
            {
                Type = entry.Type,
                MonthlyCost = _costs[entry.Type],
            };

            if (entry.Type == InsuranceType.Car && entry.RegistrationNumber is not null)
                summary.Vehicle = await _vehicleClient.GetByRegistrationAsync(entry.RegistrationNumber, cancellationToken);

            insurances.Add(summary);
        }

        return new PersonInsurancesModel
        {
            PersonalIdentificationNumber = personId,
            Insurances = insurances,
            TotalMonthlyCost = insurances.Sum(i => i.MonthlyCost),
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceServicePersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 6: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/Services/IInsuranceService.cs \
        src/IntegrationLayer.InsuranceService/Services/InsuranceService.cs \
        tests/IntegrationLayer.UnitTests/InsuranceServicePersonTests.cs
git commit -m "feat: implement GetByPersonIdAsync in InsuranceService with vehicle enrichment"
```

---

### Task 5: Register VehicleServiceClient in InsuranceService and update config

**Files:**
- Modify: `src/IntegrationLayer.InsuranceService/Program.cs`
- Modify: `src/IntegrationLayer.InsuranceService/appsettings.Development.json`

- [ ] **Step 1: Update Program.cs**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Program.cs`:

```csharp
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IInsuranceRepository, InsuranceRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApi:BaseUrl"]
        ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured."));
});

builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
});

builder.Services.AddScoped<IInsuranceService, InsuranceService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
```

- [ ] **Step 2: Add VehicleService URL to appsettings.Development.json**

Replace the full contents of `src/IntegrationLayer.InsuranceService/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ExternalApi": {
    "BaseUrl": "https://localhost:5001/"
  },
  "Services": {
    "VehicleService": "http://localhost:5200/"
  }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build IntegrationLayer.sln --verbosity quiet
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/Program.cs \
        src/IntegrationLayer.InsuranceService/appsettings.Development.json
git commit -m "feat: register VehicleServiceClient in InsuranceService DI"
```

---

### Task 6: Add person endpoint to InsuranceService InsuranceController

**Files:**
- Modify: `src/IntegrationLayer.InsuranceService/Controllers/InsuranceController.cs`
- Create: `tests/IntegrationLayer.UnitTests/InsuranceControllerPersonTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/IntegrationLayer.UnitTests/InsuranceControllerPersonTests.cs`:

```csharp
using IntegrationLayer.Core.Models;
using IntegrationLayer.InsuranceService.Controllers;
using IntegrationLayer.InsuranceService.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class InsuranceControllerPersonTests
{
    private readonly IInsuranceService _service = Substitute.For<IInsuranceService>();
    private readonly InsuranceController _sut;

    public InsuranceControllerPersonTests()
    {
        _sut = new InsuranceController(_service);
    }

    [Theory]
    [InlineData("1990010112")]        // 10 digits — too short
    [InlineData("19900101123456")]    // 14 digits — too long
    [InlineData("ABCD01011234")]      // letters
    [InlineData("199001011234567")]   // 15 chars
    public async Task GetByPersonId_ReturnsBadRequest_WhenFormatInvalid(string personId)
    {
        var result = await _sut.GetByPersonId(personId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsNotFound_WhenPersonNotFound()
    {
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((PersonInsurancesModel?)null);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsOk_WhenFound()
    {
        var model = new PersonInsurancesModel { PersonalIdentificationNumber = "199001011234" };
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByPersonId_StripsDash_BeforeCallingService()
    {
        var model = new PersonInsurancesModel { PersonalIdentificationNumber = "199001011234" };
        _service.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByPersonId("19900101-1234", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
        await _service.Received(1).GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceControllerPersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: compile error — `GetByPersonId` does not exist yet.

- [ ] **Step 3: Implement the endpoint in InsuranceService InsuranceController**

Replace the full contents of `src/IntegrationLayer.InsuranceService/Controllers/InsuranceController.cs`:

```csharp
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

    [HttpGet("person/{personId}")]
    public async Task<IActionResult> GetByPersonId(string personId, CancellationToken cancellationToken)
    {
        if (!PersonIdRegex.IsMatch(personId))
            return BadRequest("Invalid personal identification number format. Expected YYYYMMDDXXXX or YYYYMMDD-XXXX.");

        var normalized = personId.Replace("-", "");
        var result = await _service.GetByPersonIdAsync(normalized, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "InsuranceControllerPersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 7`

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/Controllers/InsuranceController.cs \
        tests/IntegrationLayer.UnitTests/InsuranceControllerPersonTests.cs
git commit -m "feat: add person insurances endpoint to InsuranceService controller"
```

---

### Task 7: Extend API gateway IInsuranceServiceClient and InsuranceServiceClient

**Files:**
- Modify: `src/IntegrationLayer.Api/Clients/IInsuranceServiceClient.cs`
- Modify: `src/IntegrationLayer.Api/Clients/InsuranceServiceClient.cs`

- [ ] **Step 1: Add GetByPersonIdAsync to IInsuranceServiceClient**

Replace the full contents of `src/IntegrationLayer.Api/Clients/IInsuranceServiceClient.cs`:

```csharp
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public interface IInsuranceServiceClient
{
    Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implement in InsuranceServiceClient**

Replace the full contents of `src/IntegrationLayer.Api/Clients/InsuranceServiceClient.cs`:

```csharp
using System.Net.Http.Json;
using IntegrationLayer.Core.Models;

namespace IntegrationLayer.Api.Clients;

public class InsuranceServiceClient : IInsuranceServiceClient
{
    private readonly HttpClient _httpClient;

    public InsuranceServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InsuranceModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<InsuranceModel>($"api/insurance/{id}", cancellationToken);
    }

    public async Task<IEnumerable<InsuranceModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<InsuranceModel>>("api/insurance", cancellationToken)
               ?? Enumerable.Empty<InsuranceModel>();
    }

    public async Task<PersonInsurancesModel?> GetByPersonIdAsync(string personId, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<PersonInsurancesModel>($"api/insurance/person/{personId}", cancellationToken);
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build IntegrationLayer.sln --verbosity quiet
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/IntegrationLayer.Api/Clients/IInsuranceServiceClient.cs \
        src/IntegrationLayer.Api/Clients/InsuranceServiceClient.cs
git commit -m "feat: add GetByPersonIdAsync to gateway InsuranceServiceClient"
```

---

### Task 8: Add person endpoint to API gateway InsuranceController

**Files:**
- Modify: `src/IntegrationLayer.Api/Controllers/InsuranceController.cs`
- Create: `tests/IntegrationLayer.UnitTests/GatewayInsuranceControllerPersonTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/IntegrationLayer.UnitTests/GatewayInsuranceControllerPersonTests.cs`:

```csharp
using IntegrationLayer.Api.Clients;
using IntegrationLayer.Api.Controllers;
using IntegrationLayer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace IntegrationLayer.UnitTests;

public class GatewayInsuranceControllerPersonTests
{
    private readonly IInsuranceServiceClient _client = Substitute.For<IInsuranceServiceClient>();
    private readonly InsuranceController _sut;

    public GatewayInsuranceControllerPersonTests()
    {
        _sut = new InsuranceController(_client);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsOk_WithModelFromClient()
    {
        var model = new PersonInsurancesModel { PersonalIdentificationNumber = "199001011234" };
        _client.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>()).Returns(model);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(model, ok.Value);
    }

    [Fact]
    public async Task GetByPersonId_ReturnsNotFound_WhenClientReturnsNull()
    {
        _client.GetByPersonIdAsync("199001011234", Arg.Any<CancellationToken>())
            .Returns((PersonInsurancesModel?)null);

        var result = await _sut.GetByPersonId("199001011234", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "GatewayInsuranceControllerPersonTests" --verbosity quiet 2>&1 | tail -5
```

Expected: compile error — `GetByPersonId` does not exist on the gateway controller yet.

- [ ] **Step 3: Implement the endpoint in the API gateway InsuranceController**

Replace the full contents of `src/IntegrationLayer.Api/Controllers/InsuranceController.cs`:

```csharp
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

    [HttpGet("person/{personId}")]
    public async Task<IActionResult> GetByPersonId(string personId, CancellationToken cancellationToken)
    {
        var result = await _client.GetByPersonIdAsync(personId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

- [ ] **Step 4: Run all unit tests**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --verbosity quiet 2>&1 | tail -5
```

Expected: all tests pass (existing 20 + new tests).

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.Api/Controllers/InsuranceController.cs \
        tests/IntegrationLayer.UnitTests/GatewayInsuranceControllerPersonTests.cs
git commit -m "feat: add person insurances endpoint to API gateway InsuranceController"
```

---

## Manual Smoke Test

Start all three services then run:

```bash
# Returns 200 with Pet + Car (Volvo XC60)
curl http://localhost:5105/api/insurance/person/199001011234

# Also works with dash
curl http://localhost:5105/api/insurance/person/19900101-1234

# Returns 200 with PersonalHealth only
curl http://localhost:5105/api/insurance/person/198505152345

# Returns 404
curl http://localhost:5105/api/insurance/person/000000000000

# Returns 400 (bad format)
curl http://localhost:5105/api/insurance/person/12345
```

Expected response for `199001011234`:
```json
{
  "personalIdentificationNumber": "199001011234",
  "insurances": [
    { "type": "Pet", "monthlyCost": 10.0, "vehicle": null },
    { "type": "Car", "monthlyCost": 30.0, "vehicle": {
        "registrationNumber": "ABC123", "make": "Volvo",
        "model": "XC60", "year": 2021, "color": "Black"
    }}
  ],
  "totalMonthlyCost": 40.0
}
```
