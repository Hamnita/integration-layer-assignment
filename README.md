# Integration Layer

A .NET 8 microservices integration layer consisting of an API gateway and two backend services.

## Architecture

```
Client
  │  X-Api-Key header required
  ▼
IntegrationLayer.Api          ← public-facing API gateway
  ├── /api/vehicle/...        → VehicleService (HTTP)
  └── /api/insurance/...      → InsuranceService (HTTP)
                                    │
                             InsuranceService also calls
                             VehicleService internally
                             to enrich insurance responses
```

**IntegrationLayer.Api** is the only entry point exposed to callers. It authenticates requests via an API key and forwards them to the appropriate internal service over HTTP.

**IntegrationLayer.VehicleService** handles vehicle registration lookups. Accepts registration numbers in the format `ABC123` (3 letters + 3 digits), normalises to uppercase, and returns make/model/year/colour.

**IntegrationLayer.InsuranceService** handles insurance lookups by person ID (Swedish personal identity number format `YYYYMMDD-XXXX` or `YYYYMMDDXXXX`). It calls VehicleService internally to resolve vehicle details for car insurances.

**IntegrationLayer.Core** is a shared class library holding the domain models used across all projects.

### Design Decisions

- **API gateway pattern** — all external traffic goes through one entry point, keeping internal services off the public network.
- **API key authentication** — a single `X-Api-Key` header is validated on every request by `ApiKeyMiddleware` before it reaches any controller. The key is read once at startup and compared using `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.
- **In-memory mock data** — both services use static dictionaries as their data store, making the project runnable with zero external dependencies.
- **Input validation at the edge** — each service validates and normalises its own inputs (registration number format, person ID format) rather than relying on the caller.

### Error Handling

All three services share the same error handling strategy via `IntegrationLayer.Core`.

- **`ExceptionMiddleware`** sits at the outermost layer of the pipeline and catches any unhandled exception, returning a `500` with a JSON body `{"error":"An unexpected error occurred."}` so raw stack traces are never exposed to callers.
- **`ApiKeyMiddleware`** returns `401` with a JSON body indicating whether the API key was missing (`{"error":"API key is missing."}`) or invalid (`{"error":"Invalid API key."}`).
- **HTTP clients** (`VehicleServiceClient`, `InsuranceServiceClient`) treat a `404` from a downstream service as a `null` result and call `EnsureSuccessStatusCode` for all other non-success responses, letting the exception middleware handle any unexpected failures.

---

## Running Locally

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Each service runs independently. Open three terminals and start them in any order.

**VehicleService** (port 5200):
```bash
dotnet run --project src/IntegrationLayer.VehicleService
```

**InsuranceService** (port 5300):
```bash
dotnet run --project src/IntegrationLayer.InsuranceService
```

**API Gateway** (port 5105):
```bash
dotnet run --project src/IntegrationLayer.Api
```

Set the service URLs and API key via environment variables or `appsettings.Development.json`.

### Environment Variables

| Variable | Example | Description |
|----------|---------|-------------|
| `ApiKey` | `change-me-in-production` | API key required on all gateway requests |
| `Services__VehicleService` | `http://localhost:5200/` | Gateway → VehicleService URL |
| `Services__InsuranceService` | `http://localhost:5300/` | Gateway → InsuranceService URL |

> Note: ASP.NET Core maps `__` in environment variable names to `:` in configuration keys.

### Windows (PowerShell)

```powershell
$env:ApiKey = "change-me-in-production"
$env:Services__VehicleService = "http://localhost:5200/"
$env:Services__InsuranceService = "http://localhost:5300/"
dotnet run --project src/IntegrationLayer.Api
```

### WSL / Linux

```bash
ApiKey="change-me-in-production" Services__VehicleService="http://localhost:5200/" Services__InsuranceService="http://localhost:5300/" dotnet run --project src/IntegrationLayer.Api
```

### macOS

```bash
ApiKey="change-me-in-production" Services__VehicleService="http://localhost:5200/" Services__InsuranceService="http://localhost:5300/" dotnet run --project src/IntegrationLayer.Api
```

### Running the Tests

```bash
dotnet test
```

### Example Request

```bash
curl -H "X-Api-Key: change-me-in-production" http://localhost:5105/api/vehicle/registration/ABC123
```

### Personal reflection:
  # Any similar project or experience you’ve had in the past:
    In my last assignment i worked with a brand new application for electricians and the work also involved breaking up an old monolith gateway into microservices to create, read, update and delete data for members, companies etc in many different sources (both internal and external).

  # What was challenging or interesting in this assignment:
    There is alot of elements to creating a new intergration layer, it's interesting to have to think about security, architecture, SoC and also to keep it simple and not overcomplicate things.

  # What you would improve or extend if you had more time:
    Currently there is only build & test in GitHub, next step would be to set up a complete CI/CD pipeline and environments for dev, test, stage & prod. Codewise i would probably focus on setting a more explained code guideline and set a 
