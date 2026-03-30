# Person Insurances Lookup — Design Spec

**Date:** 2026-03-31
**Status:** Approved

## Overview

Add a `GET /api/insurance/person/{personId}` endpoint that accepts a Swedish personal identification number (personnummer), returns all insurances the person holds with their monthly costs, and enriches any car insurance entries with vehicle information fetched from VehicleService.

## Architecture & Data Flow

```
Client
  → GET /api/insurance/person/{personId}          (IntegrationLayer.Api)
      → IInsuranceServiceClient.GetByPersonIdAsync()
          → GET /api/insurance/person/{personId}   (IntegrationLayer.InsuranceService)
              → IInsuranceService.GetByPersonIdAsync()
                  → IInsuranceRepository.GetByPersonIdAsync()   ← in-memory mock
                  → for each car insurance:
                      IVehicleServiceClient.GetByRegistrationAsync()
                      → GET /api/vehicle/registration/{regNr}   (IntegrationLayer.VehicleService)
```

## Validation

- `personId` must match `^\d{8}-?\d{4}$` — 8 digits, optional dash, 4 digits (e.g. `199001011234` or `19900101-1234`).
- The dash is stripped before lookup so both formats resolve to the same key.
- Validation performed in the InsuranceService controller.
- Invalid format → `400 Bad Request`
- Person not found → `404 Not Found`
- Found → `200 OK` with `PersonInsurancesModel`

## Monthly Costs (fixed constants)

| Insurance Type   | Monthly Cost |
|------------------|-------------|
| Pet              | $10.00      |
| PersonalHealth   | $20.00      |
| Car              | $30.00      |

Costs are assigned in `InsuranceService`, not stored in the repository.

If VehicleService returns `null` for a car insurance's registration number (e.g. not found in mock), the insurance is still included in the response with `Vehicle = null`.

## Data Models

### New in `IntegrationLayer.Core/Models`

**`InsuranceType.cs`**
```csharp
public enum InsuranceType { Pet, PersonalHealth, Car }
```

**`InsuranceSummaryModel.cs`**
```csharp
public class InsuranceSummaryModel
{
    public InsuranceType Type { get; set; }
    public decimal MonthlyCost { get; set; }
    public VehicleRegistrationModel? Vehicle { get; set; } // only populated for Car
}
```

**`PersonInsurancesModel.cs`**
```csharp
public class PersonInsurancesModel
{
    public string PersonalIdentificationNumber { get; set; } = string.Empty;
    public IEnumerable<InsuranceSummaryModel> Insurances { get; set; } = [];
    public decimal TotalMonthlyCost { get; set; }
}
```

## Mock Data

`InsuranceRepository` holds a static in-memory dictionary mapping normalized personnummer (12 digits, no dash) to a list of `(InsuranceType, string? RegistrationNumber)` tuples:

| PersonId       | Insurances                                      |
|----------------|-------------------------------------------------|
| 199001011234   | Pet, Car (ABC123)                               |
| 198505152345   | PersonalHealth                                  |
| 200203033456   | Pet, PersonalHealth, Car (XYZ789)               |

## Files Changed

### IntegrationLayer.Core
- `Models/InsuranceType.cs` — new enum
- `Models/InsuranceSummaryModel.cs` — new model
- `Models/PersonInsurancesModel.cs` — new model

### IntegrationLayer.InsuranceService
- `Clients/IVehicleServiceClient.cs` — new local interface for calling VehicleService
- `Clients/VehicleServiceClient.cs` — new local HTTP client implementation
- `Repositories/IInsuranceRepository.cs` — add `GetByPersonIdAsync`
- `Repositories/InsuranceRepository.cs` — implement with in-memory mock
- `Services/IInsuranceService.cs` — add `GetByPersonIdAsync`
- `Services/InsuranceService.cs` — implement: fetch from repo, assign costs, enrich car insurances
- `Controllers/InsuranceController.cs` — add `GET /api/insurance/person/{personId}` with validation
- `Program.cs` — register `IVehicleServiceClient` / `VehicleServiceClient` with VehicleService URL from config
- `appsettings.Development.json` — add `Services:VehicleService` URL (`http://localhost:5200/`)

### IntegrationLayer.Api
- `Clients/IInsuranceServiceClient.cs` — add `GetByPersonIdAsync`
- `Clients/InsuranceServiceClient.cs` — implement HTTP forwarding
- `Controllers/InsuranceController.cs` — add `GET /api/insurance/person/{personId}`, proxy to client

## Example Response

```json
GET /api/insurance/person/199001011234

{
  "personalIdentificationNumber": "199001011234",
  "insurances": [
    {
      "type": "Pet",
      "monthlyCost": 10.00,
      "vehicle": null
    },
    {
      "type": "Car",
      "monthlyCost": 30.00,
      "vehicle": {
        "registrationNumber": "ABC123",
        "make": "Volvo",
        "model": "XC60",
        "year": 2021,
        "color": "Black"
      }
    }
  ],
  "totalMonthlyCost": 40.00
}
```

## No New Projects

No new microservice projects needed. `InsuranceService` gets its own local `IVehicleServiceClient` / `VehicleServiceClient` (same pattern as VehicleService's own local interfaces — no cross-project dependency).
