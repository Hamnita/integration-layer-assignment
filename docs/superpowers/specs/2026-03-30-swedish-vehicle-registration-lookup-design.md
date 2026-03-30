# Swedish Vehicle Registration Lookup — Design Spec

**Date:** 2026-03-30
**Status:** Approved

## Overview

Add a new endpoint that accepts a Swedish vehicle registration number and returns basic vehicle information (make, model, year, color). Data is served from an in-memory mock — no real external API integration.

## Architecture

The feature follows the existing layered microservice pattern:

```
Client
  → GET /api/vehicle/registration/{regNr}   (IntegrationLayer.Api gateway)
      → IVehicleServiceClient.GetByRegistrationAsync()
          → GET /api/vehicle/registration/{regNr}   (IntegrationLayer.VehicleService)
              → IVehicleService.GetByRegistrationAsync()
                  → IVehicleRepository.GetByRegistrationAsync()
                      → in-memory mock list (VehicleRepository)
```

## Validation

- Registration number must match the pattern `^[A-Za-z]{3}[0-9]{3}$` (3 letters + 3 digits, case-insensitive).
- The value is normalized to uppercase before being passed to the service layer.
- Validation is performed in the VehicleService controller before calling the service layer.
- Invalid format → `400 Bad Request`
- Not found in mock data → `404 Not Found`
- Found → `200 OK` with `VehicleRegistrationModel`

## Data Model

New model added to `IntegrationLayer.Core/Models/VehicleRegistrationModel.cs`:

```csharp
public class VehicleRegistrationModel
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
}
```

## Files Changed

### IntegrationLayer.Core
- `Models/VehicleRegistrationModel.cs` — new model (see above)

### IntegrationLayer.VehicleService
- `Repositories/IVehicleRepository.cs` — add `GetByRegistrationAsync(string regNr, CancellationToken)`
- `Repositories/VehicleRepository.cs` — implement with hardcoded in-memory mock entries
- `Services/IVehicleService.cs` — add `GetByRegistrationAsync(string regNr, CancellationToken)`
- `Services/VehicleService.cs` — delegate to repository
- `Controllers/VehicleController.cs` — add `GET /api/vehicle/registration/{regNr}` action

### IntegrationLayer.Api
- `Clients/IVehicleServiceClient.cs` — add `GetByRegistrationAsync(string regNr, CancellationToken)`
- `Clients/VehicleServiceClient.cs` — implement HTTP call forwarding to VehicleService
- `Controllers/VehicleController.cs` — add `GET /api/vehicle/registration/{regNr}` action, proxies to client

## Mock Data

A small static list in `VehicleRepository`, e.g.:

| Registration | Make   | Model  | Year | Color  |
|-------------|--------|--------|------|--------|
| ABC123      | Volvo  | XC60   | 2021 | Black  |
| XYZ789      | Saab   | 9-3    | 2008 | Silver |
| DEF456      | Volvo  | V70    | 2015 | White  |

## No New Projects or DI Changes

No new microservice projects are needed. No new DI registrations are required — `VehicleRepository` and `VehicleService` are already registered in `IntegrationLayer.VehicleService/Program.cs`, and `VehicleServiceClient` is already registered in `IntegrationLayer.Api/Program.cs`.
