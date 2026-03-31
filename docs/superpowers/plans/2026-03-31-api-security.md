# API Security Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add API key authentication middleware to `IntegrationLayer.Api` so all requests without a valid `X-Api-Key` header are rejected with `401 Unauthorized`.

**Architecture:** A single `ApiKeyMiddleware` class implementing `IMiddleware` is registered as a scoped service and inserted into the pipeline before `UseAuthorization`. It reads the `X-Api-Key` header and compares it against a value from `IConfiguration`. Internal microservices remain unsecured (network-trusted).

**Tech Stack:** .NET 8, ASP.NET Core, xUnit, NSubstitute

---

## File Map

| Action | File | Responsibility |
|--------|------|----------------|
| Create | `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs` | Validates `X-Api-Key` header on every request |
| Modify | `src/IntegrationLayer.Api/Program.cs` | Register and use `ApiKeyMiddleware` |
| Modify | `src/IntegrationLayer.Api/appsettings.json` | Add `ApiKey` config entry |
| Create | `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs` | Unit tests for middleware logic |
| Create | `tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs` | End-to-end tests through the full pipeline |

---

### Task 1: Add ApiKey to configuration

**Files:**
- Modify: `src/IntegrationLayer.Api/appsettings.json`

- [ ] **Step 1: Add the ApiKey entry to appsettings.json**

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
  "ApiKey": "change-me-in-production"
}
```

- [ ] **Step 2: Commit**

```bash
git add src/IntegrationLayer.Api/appsettings.json
git commit -m "config: add ApiKey placeholder to appsettings"
```

---

### Task 2: ApiKeyMiddleware — unit tests first (TDD)

**Files:**
- Create: `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs`
- Create: `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs`

- [ ] **Step 1: Write the failing unit tests**

Create `tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs`:

```csharp
using IntegrationLayer.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace IntegrationLayer.UnitTests;

public class ApiKeyMiddlewareTests
{
    private const string ValidApiKey = "test-key-123";
    private readonly ApiKeyMiddleware _sut;

    public ApiKeyMiddlewareTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiKey"] = ValidApiKey })
            .Build();

        _sut = new ApiKeyMiddleware(configuration);
    }

    [Fact]
    public async Task InvokeAsync_Returns401_WhenApiKeyHeaderMissing()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Returns401_WhenApiKeyHeaderValueIsWrong()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "wrong-key";
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenApiKeyHeaderIsCorrect()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = ValidApiKey;
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await _sut.InvokeAsync(context, next);

        Assert.True(nextCalled);
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure (class not yet created)**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "FullyQualifiedName~ApiKeyMiddlewareTests"
```

Expected: Build error — `ApiKeyMiddleware` does not exist yet.

- [ ] **Step 3: Create the middleware**

Create `src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace IntegrationLayer.Api.Middleware;

public class ApiKeyMiddleware : IMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var apiKey = _configuration["ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is not configured.");

        if (!apiKey.Equals(extractedApiKey.ToString(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
```

- [ ] **Step 4: Run tests — expect all three to pass**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj --filter "FullyQualifiedName~ApiKeyMiddlewareTests"
```

Expected output:
```
Passed!  - Failed: 0, Passed: 3, Skipped: 0
```

- [ ] **Step 5: Commit**

```bash
git add src/IntegrationLayer.Api/Middleware/ApiKeyMiddleware.cs \
        tests/IntegrationLayer.UnitTests/ApiKeyMiddlewareTests.cs
git commit -m "feat: add ApiKeyMiddleware with unit tests"
```

---

### Task 3: Register middleware in Program.cs

**Files:**
- Modify: `src/IntegrationLayer.Api/Program.cs`

- [ ] **Step 1: Register ApiKeyMiddleware as a scoped service and add it to the pipeline**

Replace the contents of `src/IntegrationLayer.Api/Program.cs` with:

```csharp
using IntegrationLayer.Api.Clients;
using IntegrationLayer.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP client to VehicleService microservice
builder.Services.AddHttpClient<IVehicleServiceClient, VehicleServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:VehicleService"]
        ?? throw new InvalidOperationException("Services:VehicleService is not configured."));
});

// HTTP client to InsuranceService microservice
builder.Services.AddHttpClient<IInsuranceServiceClient, InsuranceServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InsuranceService"]
        ?? throw new InvalidOperationException("Services:InsuranceService is not configured."));
});

builder.Services.AddScoped<ApiKeyMiddleware>();

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

- [ ] **Step 2: Run all unit tests to confirm nothing is broken**

```bash
dotnet test tests/IntegrationLayer.UnitTests/IntegrationLayer.UnitTests.csproj
```

Expected output:
```
Passed!  - Failed: 0
```

- [ ] **Step 3: Commit**

```bash
git add src/IntegrationLayer.Api/Program.cs
git commit -m "feat: register and use ApiKeyMiddleware in pipeline"
```

---

### Task 4: Integration tests

**Files:**
- Create: `tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs`

- [ ] **Step 1: Write the integration tests**

Create `tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs`:

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
                    ["Services:VehicleService"] = "http://localhost:5001/",
                    ["Services:InsuranceService"] = "http://localhost:5002/"
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

- [ ] **Step 2: Run integration tests — expect all three to pass**

```bash
dotnet test tests/IntegrationLayer.IntegrationTests/IntegrationLayer.IntegrationTests.csproj --filter "FullyQualifiedName~ApiKeyMiddlewareIntegrationTests"
```

Expected output:
```
Passed!  - Failed: 0, Passed: 3, Skipped: 0
```

Note: The third test (`DoesNotReturn401`) will return a non-401 status (likely `500` or similar) because the downstream microservices are not running — this is expected and correct. The test only asserts the middleware does not block the request.

- [ ] **Step 3: Run the full test suite**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/IntegrationLayer.IntegrationTests/ApiKeyMiddlewareIntegrationTests.cs
git commit -m "test: add integration tests for ApiKeyMiddleware"
```
