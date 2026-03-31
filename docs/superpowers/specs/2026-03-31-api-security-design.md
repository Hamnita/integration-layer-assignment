# API Security Design

**Date:** 2026-03-31
**Status:** Approved

## Summary

Add API key authentication to `IntegrationLayer.Api` (the public-facing gateway). Internal microservices (`VehicleService`, `InsuranceService`) are network-trusted and require no additional security.

## Scope

- **In scope:** External authentication on `IntegrationLayer.Api`
- **Out of scope:** Service-to-service security (network trust assumed), identity providers, authorization policies, key rotation

## Architecture

A single ASP.NET Core middleware class (`ApiKeyMiddleware`) enforces authentication globally on all incoming requests to the API gateway. The middleware is registered in `Program.cs` before the existing authorization and routing middleware.

The API key is stored in `appsettings.json` and overridable via environment variable or secrets manager in production.

## Components

### `Middleware/ApiKeyMiddleware.cs` (new file)

- Implements `IMiddleware`
- Reads the `X-Api-Key` request header
- Compares against `IConfiguration["ApiKey"]`
- Returns `401 Unauthorized` if the header is missing or the value does not match
- Calls `next(context)` on success

### `Program.cs` (modified)

- Register `ApiKeyMiddleware` as a scoped service
- Call `app.UseMiddleware<ApiKeyMiddleware>()` before `app.UseAuthorization()` and `app.MapControllers()`

### `appsettings.json` (modified)

- Add `"ApiKey": "<value>"` entry

## Data Flow

```
Client Request
  → ApiKeyMiddleware
      → missing/invalid X-Api-Key header → 401 Unauthorized
      → valid key → UseAuthorization → MapControllers → Controller → Microservice
```

## Error Handling

- Missing header: `401 Unauthorized`
- Wrong key value: `401 Unauthorized`
- No response body — avoids leaking information about the expected format

## Testing

- Unit test: middleware returns 401 when header is absent
- Unit test: middleware returns 401 when header value is wrong
- Unit test: middleware calls next when header value is correct
- Integration test: end-to-end request with and without a valid key
