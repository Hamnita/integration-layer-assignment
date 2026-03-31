# Internal Service Security Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Secure VehicleService and InsuranceService so only the API gateway can call them, using a separate internal API key forwarded automatically by a `DelegatingHandler`.

**Architecture:** `ApiKeyMiddleware` moves from `IntegrationLayer.Api` to `IntegrationLayer.Core` so all three services can share it. A new `InternalApiKeyHandler` (also in Core) is a `DelegatingHandler` that injects the internal `X-Api-Key` header on every outbound HTTP call from the gateway and from InsuranceService. Each internal service registers `ApiKeyMiddleware` with its own `ApiKey` config value, keeping it completely separate from the external key on the gateway.

**Tech Stack:** .NET 8, ASP.NET Core, xUnit, NSubstitute

---

## File Map

| Action | File | Responsibility |
|--------|------|----------------|
| Modify | `src/IntegrationLayer.Core/IntegrationLayer.Core.csproj` | Add ASP.NET Core framework reference |
| Create | `src/IntegrationLayer.Core/Middleware/ApiKeyMiddleware.cs` | Shared middleware (moved from Api) |
| Create | `src/IntegrationLayer.Core/Middleware/InternalApiKeyHandler.cs` | Injects internal key on outbound HTTP calls |
| Delete | `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs` | Replaced by Core version |
| Modify | `src/IntegrationLayer.Api/Program.cs` | Update using, register handler, add to HttpClients |
| Modify | `src/IntegrationLayer.Api/appsettings.json` | Add `Services:InternalApiKey` |
| Modify | `src/IntegrationLayer.VehicleService/Program.cs` | Register ApiKeyMiddleware |
| Modify | `src/IntegrationLayer.VehicleService/appsettings.json` | Add `ApiKey` |
| Modify | `src/IntegrationLayer.InsuranceService/Program.cs` | Register ApiKeyMiddleware + InternalApiKeyHandler |
| Modify | `src/IntegrationLayer.InsuranceService/appsettings.json` | Add `ApiKey` + `Services:InternalApiKey` |
| Modify | `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs` | Update namespace import |
| Create | `tests/IntegrationLayer.UnitTests/InternalApiKeyHandlerTests.cs` | Unit tests for new handler |

---

### Task 1: Extend Core with shared middleware

**Files:**
- Modify: `src/IntegrationLayer.Core/IntegrationLayer.Core.csproj`
- Create: `src/IntegrationLayer.Core/Middleware/ApiKeyMiddleware.cs`
- Create: `src/IntegrationLayer.Core/Middleware/InternalApiKeyHandler.cs`

- [ ] **Step 1: Add ASP.NET Core framework reference to Core.csproj**

Replace the contents of `src/IntegrationLayer.Core/IntegrationLayer.Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create ApiKeyMiddleware in Core**

Create `src/IntegrationLayer.Core/Middleware/ApiKeyMiddleware.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace IntegrationLayer.Core.Middleware;

public class ApiKeyMiddleware : IMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly byte[] _apiKeyBytes;

    public ApiKeyMiddleware(IConfiguration configuration)
    {
        var apiKey = configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is not configured.");
        _apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var extractedBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());
        if (!CryptographicOperations.FixedTimeEquals(_apiKeyBytes, extractedBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
```

- [ ] **Step 3: Create InternalApiKeyHandler in Core**

Create `src/IntegrationLayer.Core/Middleware/InternalApiKeyHandler.cs`:

```csharp
namespace IntegrationLayer.Core.Middleware;

public class InternalApiKeyHandler : DelegatingHandler
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly string _apiKey;

    public InternalApiKeyHandler(IConfiguration configuration)
    {
        _apiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
```

- [ ] **Step 4: Build Core to verify it compiles**

```bash
/home/hamne/.dotnet/dotnet build src/IntegrationLayer.Core 2>&1
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.Core/
git commit -m "feat: add ApiKeyMiddleware and InternalApiKeyHandler to Core"
```

---

### Task 2: Update unit tests

**Files:**
- Modify: `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs`
- Create: `tests/IntegrationLayer.UnitTests/InternalApiKeyHandlerTests.cs`

- [ ] **Step 1: Write failing InternalApiKeyHandlerTests**

Create `tests/IntegrationLayer.UnitTests/InternalApiKeyHandlerTests.cs`:

```csharp
using IntegrationLayer.Core.Middleware;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.UnitTests;

public class InternalApiKeyHandlerTests
{
    private const string TestInternalKey = "internal-key-456";

    [Fact]
    public void Constructor_Throws_WhenInternalApiKeyNotConfigured()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new InternalApiKeyHandler(emptyConfig));
    }

    [Fact]
    public async Task SendAsync_AddsApiKeyHeader_WithConfiguredValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Services:InternalApiKey"] = TestInternalKey })
            .Build();

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new TestMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new InternalApiKeyHandler(config) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal(TestInternalKey, capturedRequest.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task SendAsync_DoesNotOverwriteExistingHeader()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Services:InternalApiKey"] = TestInternalKey })
            .Build();

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new TestMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new InternalApiKeyHandler(config) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://test/");
        request.Headers.TryAddWithoutValidation("X-Api-Key", "already-set");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        // TryAddWithoutValidation does not overwrite — original value preserved
        Assert.Contains("already-set", capturedRequest.Headers.GetValues("X-Api-Key"));
    }
}

internal class TestMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public TestMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
```

- [ ] **Step 2: Run the new tests — expect all 3 to pass**

```bash
/home/hamne/.dotnet/dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "FullyQualifiedName~InternalApiKeyHandlerTests" 2>&1
```

Expected: `Passed! - Failed: 0, Passed: 3` — Core is already referenced by the unit test project so `InternalApiKeyHandler` is immediately visible.

- [ ] **Step 3: Update ApiKeyMiddlewareTests.cs namespace import**

Replace line 1 of `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs`:

```csharp
using IntegrationLayer.Core.Middleware;
```

(was `using IntegrationLayer.Api.Middleware;`)

- [ ] **Step 4: Run all unit tests — expect all to pass**

```bash
/home/hamne/.dotnet/dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj 2>&1
```

Expected: `Passed! - Failed: 0, Passed: 42` (39 existing + 3 new)

- [ ] **Step 5: Commit**

```bash
git add tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs \
        tests/IntegrationLayer.UnitTests/InternalApiKeyHandlerTests.cs
git commit -m "test: add InternalApiKeyHandler tests, update ApiKeyMiddleware namespace"
```

---

### Task 3: Update Api — remove old middleware, wire new handler

**Files:**
- Delete: `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs`
- Modify: `src/IntegrationLayer.Api/Program.cs`
- Modify: `src/IntegrationLayer.Api/appsettings.json`

- [ ] **Step 1: Delete the old ApiKeyMiddleware from Api**

Delete `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs`.

- [ ] **Step 2: Replace Api Program.cs**

Replace the full contents of `src/IntegrationLayer.Api/Program.cs` with:

```csharp
using IntegrationLayer.Api.Clients;
using IntegrationLayer.Core.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "API key required for all requests"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            []
        }
    });
});

builder.Services.AddTransient<InternalApiKeyHandler>();

// HTTP client to VehicleService microservice
builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

// HTTP client to InsuranceService microservice
builder.Services.AddHttpClient<IInsuranceServiceClient, InsuranceServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InsuranceService"]
        ?? throw new InvalidOperationException("Services:InsuranceService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

builder.Services.AddSingleton<ApiKeyMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
```

- [ ] **Step 3: Update Api appsettings.json**

Replace the contents of `src/IntegrationLayer.Api/appsettings.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKey": "change-me-in-production",
  "Services": {
    "InternalApiKey": "change-me-in-production"
  }
}
```

- [ ] **Step 4: Run all unit tests to verify nothing broke**

```bash
/home/hamne/.dotnet/dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj 2>&1
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.Api/
git commit -m "feat: wire InternalApiKeyHandler into Api HttpClients"
```

---

### Task 4: Secure VehicleService

**Files:**
- Modify: `src/IntegrationLayer.VehicleService/Program.cs`
- Modify: `src/IntegrationLayer.VehicleService/appsettings.json`

- [ ] **Step 1: Update VehicleService appsettings.json**

Replace the contents of `src/IntegrationLayer.VehicleService/appsettings.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKey": "change-me-in-production"
}
```

- [ ] **Step 2: Update VehicleService Program.cs**

Replace the contents of `src/IntegrationLayer.VehicleService/Program.cs` with:

```csharp
using IntegrationLayer.Core.Middleware;
using IntegrationLayer.VehicleService.Repositories;
using IntegrationLayer.VehicleService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddSingleton<ApiKeyMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
```

- [ ] **Step 3: Build VehicleService**

```bash
/home/hamne/.dotnet/dotnet build src/IntegrationLayer.VehicleService 2>&1
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Run all unit tests**

```bash
/home/hamne/.dotnet/dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj 2>&1
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.VehicleService/
git commit -m "feat: secure VehicleService with ApiKeyMiddleware"
```

---

### Task 5: Secure InsuranceService

**Files:**
- Modify: `src/IntegrationLayer.InsuranceService/Program.cs`
- Modify: `src/IntegrationLayer.InsuranceService/appsettings.json`

- [ ] **Step 1: Update InsuranceService appsettings.json**

Replace the contents of `src/IntegrationLayer.InsuranceService/appsettings.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKey": "change-me-in-production",
  "Services": {
    "InternalApiKey": "change-me-in-production"
  }
}
```

- [ ] **Step 2: Update InsuranceService Program.cs**

Replace the contents of `src/IntegrationLayer.InsuranceService/Program.cs` with:

```csharp
using IntegrationLayer.Core.Middleware;
using IntegrationLayer.InsuranceService.Clients;
using IntegrationLayer.InsuranceService.Repositories;
using IntegrationLayer.InsuranceService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();

builder.Services.AddTransient<InternalApiKeyHandler>();
builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
}).AddHttpMessageHandler<InternalApiKeyHandler>();

builder.Services.AddScoped<IInsuranceService, InsuranceService>();
builder.Services.AddSingleton<ApiKeyMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
```

- [ ] **Step 3: Build InsuranceService**

```bash
/home/hamne/.dotnet/dotnet build src/IntegrationLayer.InsuranceService 2>&1
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Run full test suite**

```bash
/home/hamne/.dotnet/dotnet test 2>&1 | tail -10
```

Expected: All tests pass, 0 failures.

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.InsuranceService/
git commit -m "feat: secure InsuranceService with ApiKeyMiddleware and InternalApiKeyHandler"
```

---

### Task 6: Update integration tests configuration

**Files:**
- Modify: `tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs`

The existing integration tests use `WebApplicationFactory<Program>` for the Api gateway. They configure `Services:VehicleService` and `Services:InsuranceService` but not `Services:InternalApiKey` — the gateway will now throw at startup if this is missing.

- [ ] **Step 1: Add InternalApiKey to integration test configuration**

Replace the `ConfigureAppConfiguration` block in `tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs` with:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.IntegrationTests;

public class ApiKeyMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "integration-test-key";
    private readonly WebApplicationFactory<Program> _factory;

    public ApiKeyMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKey"] = TestApiKey,
                    ["Services:VehicleService"] = "http://localhost:5200/",
                    ["Services:InsuranceService"] = "http://localhost:5300/",
                    ["Services:InternalApiKey"] = "internal-test-key"
                });
            });
        });
    }

    [Fact]
    public async Task Request_Returns401_WhenApiKeyHeaderMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_Returns401_WhenApiKeyHeaderIsWrong()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_DoesNotReturn401_WhenApiKeyHeaderIsCorrect()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);

        var response = await client.GetAsync("/api/vehicle/registration/ABC123");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run full test suite**

```bash
/home/hamne/.dotnet/dotnet test 2>&1 | tail -10
```

Expected: All tests pass, 0 failures.

- [ ] **Step 3: Commit**

```bash
git add tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs
git commit -m "test: add Services:InternalApiKey to integration test config"
```
