# Internal Service Security Design

**Date:** 2026-03-31
**Status:** Approved

## Summary

Secure `VehicleService` and `InsuranceService` so they only accept requests from the API gateway, not from external callers. Uses a separate internal API key, distinct from the external-facing key on the gateway.

## Scope

- **In scope:** Authentication on VehicleService and InsuranceService; gateway forwarding the internal key on outbound requests
- **Out of scope:** Key rotation, per-service keys, mTLS, network-level isolation

## Architecture

`ApiKeyMiddleware` is moved from `IntegrationLayer.Api` into `IntegrationLayer.Core` so all three services share the same implementation. Each service registers it independently with its own `ApiKey` config value.

The gateway uses a `DelegatingHandler` (`InternalApiKeyHandler`) attached to both HTTP clients to automatically inject the `X-Api-Key` header on every outbound request to the internal services.

```
External Caller
  │  X-Api-Key: <external key>
  ▼
IntegrationLayer.Api (ApiKeyMiddleware validates external key)
  │  X-Api-Key: <internal key>  ← injected by InternalApiKeyHandler
  ├──▶ VehicleService (ApiKeyMiddleware validates internal key)
  └──▶ InsuranceService (ApiKeyMiddleware validates internal key)
              │  X-Api-Key: <internal key>
              └──▶ VehicleService (same internal key)
```

## Components

### `IntegrationLayer.Core` (modified)

- Move `ApiKeyMiddleware.cs` here from `IntegrationLayer.Api/Middleware/`
- Namespace: `IntegrationLayer.Core.Middleware`

### `IntegrationLayer.Api` (modified)

- Remove `Middleware/ApiKeyMiddleware.cs` (now in Core)
- Update `using` to `IntegrationLayer.Core.Middleware`
- Add `Middleware/InternalApiKeyHandler.cs` — a `DelegatingHandler` that reads `Services:InternalApiKey` from config and adds `X-Api-Key` header to all outbound requests
- Register `InternalApiKeyHandler` on both `HttpClient` registrations in `Program.cs`
- Add `"Services": { "InternalApiKey": "change-me-in-production" }` to `appsettings.json`

### `IntegrationLayer.VehicleService` (modified)

- Add project reference to `IntegrationLayer.Core`
- Register `ApiKeyMiddleware` as singleton in `Program.cs`
- Add `app.UseMiddleware<ApiKeyMiddleware>()` before `UseAuthorization`
- Add `"ApiKey": "change-me-in-production"` to `appsettings.json`

### `IntegrationLayer.InsuranceService` (modified)

- Already references `IntegrationLayer.Core` (via `IVehicleServiceClient`)
- Register `ApiKeyMiddleware` as singleton in `Program.cs`
- Add `app.UseMiddleware<ApiKeyMiddleware>()` before `UseAuthorization`
- Add `"ApiKey": "change-me-in-production"` to `appsettings.json`
- `InternalApiKeyHandler` must also be registered on its outbound `VehicleServiceClient` so InsuranceService→VehicleService calls are authenticated

## Config Summary

| Service | Key | Purpose |
|---------|-----|---------|
| Api | `ApiKey` | Validates inbound external requests |
| Api | `Services:InternalApiKey` | Forwarded to internal services |
| VehicleService | `ApiKey` | Validates inbound requests (from gateway or InsuranceService) |
| InsuranceService | `ApiKey` | Validates inbound requests (from gateway) |

External key and internal key are separate values.

## Error Handling

- Missing or wrong `X-Api-Key` on internal services → `401 Unauthorized` (same as gateway)
- Missing `Services:InternalApiKey` config on gateway → `InvalidOperationException` at startup

## Testing

- Unit tests: `InternalApiKeyHandler` adds the correct header; does not add header when config key missing
- Existing `ApiKeyMiddleware` unit tests remain valid (now in Core)
- Integration tests: direct call to VehicleService without key → 401; call via gateway with valid keys → success
