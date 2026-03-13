# Currency Converter API

> 🌐 **Live Demo** — you can test the full stack here:
>
> | | URL |
> |---|---|
> | **API** | [https://converter-dev.rahmatov.net](https://converter-dev.rahmatov.net) |
> | **Frontend** | [https://converter-dev-front.rahmatov.net](https://converter-dev-front.rahmatov.net) |
>
> CI/CD is configured on the `dev` branch — every push deploys automatically to the URLs above.
>
> **Test credentials:**
> ```json
> { "username": "testuser", "password": "Test@1234" }
> ```
>
> 📄 Full API documentation is available on this GitHub repository.

---

## How I Used AI (Copilot) for This Project

### Backend

I already had a **DDD architecture** on my GitHub profile, so I asked Copilot to scaffold a new project with all layers taken into account. I used the **GitHub MCP server** to give Copilot direct access to my existing repositories so it could generate everything according to my established patterns. I then asked it to integrate the Frankfurter API and provided the required details.

Features were added in **multiple iterations** to ensure each one was correct and working before moving on — caching, retry policy, circuit breaker, factory pattern, rate limiter, authentication, and observability (Serilog, OpenTelemetry, Grafana). I also use my **own `TheTechLoop.HybridCache` library**, which makes it straightforward to switch to Redis if needed in the future. Once the core was complete, I asked Copilot to generate the unit tests, then reviewed them manually to verify they made sense and that coverage was satisfactory.

### Frontend

Once the backend API was fully ready, I asked Copilot to generate a **React app** based on the existing backend. I provided the requirements for the frontend. After everything was ready I tested it with the test credentials — it worked as expected except for a few minor UI tweaks (such as showing the currency symbol in label titles) which I fixed myself.

I then set up **CI/CD using Coolify** on my own server, added the required environment variables, and deployed both projects.

> **Note:** I did not accept AI-generated code blindly. I always reviewed what was generated and used AI to *critique* its own output — this almost always surfaced a better solution or an optimization opportunity.

---

A production-ready **Currency Converter REST API** built with **.NET 10** and **Clean Architecture**. It wraps the [Frankfurter](https://www.frankfurter.app/) public exchange-rate service and adds JWT authentication, rate limiting, caching, resilience policies, structured logging, and OpenTelemetry observability.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Run Locally](#run-locally)
  - [Run with Docker](#run-with-docker)
- [Configuration Reference](#configuration-reference)
  - [Serilog](#serilog)
  - [OpenTelemetry](#opentelemetry)
  - [ExchangeRateProvider](#exchangerateprovider)
  - [Frankfurter](#frankfurter)
  - [JwtSettings](#jwtsettings)
  - [RateLimiting](#ratelimiting)
  - [Cors](#cors)
  - [TheTechLoopCache](#thetechloopcache)
- [API Versioning](#api-versioning)
- [Authentication](#authentication)
- [Rate Limiting](#rate-limiting)
- [Caching](#caching)
- [Resilience](#resilience)
- [Observability](#observability)
- [Error Handling](#error-handling)
- [Testing](#testing)

---

## Features

| Feature | Details |
|---|---|
| **Exchange Rates** | Latest rates, historical rates by date, historical rates over a date range (paginated) |
| **Currency Conversion** | Convert an amount between any supported currency pair(s) |
| **Available Currencies** | List all currencies supported by Frankfurter |
| **JWT Authentication** | HMAC-SHA256 (symmetric) or RSA (asymmetric) bearer token validation |
| **Rate Limiting** | Fixed-window per-user, per-IP on auth endpoint, and global IP ceiling |
| **Caching** | In-memory or hybrid (memory + Redis) via `TheTechLoop.HybridCache` with MediatR integration |
| **Resilience** | Polly standard resilience handler: retries with exponential back-off + jitter, circuit breaker |
| **Validation** | FluentValidation via MediatR pipeline behavior — returns `422 Unprocessable Entity` on failure |
| **Structured Logging** | Serilog with console and Grafana Loki sinks; enriched with machine name, thread, process, environment |
| **Distributed Tracing** | OpenTelemetry traces exported to Grafana Tempo via OTLP |
| **Metrics** | ASP.NET Core, HttpClient, and .NET runtime metrics scraped by Prometheus |
| **Health Checks** | `/health` (liveness) and `/health/ready` endpoints |
| **API Versioning** | URL-segment versioning (`/api/v1/...`). Per-version OpenAPI documents with a Scalar drop-down selector. Adding v2 requires changing two lines. |
| **OpenAPI / Scalar UI** | Per-version docs with Bearer auth; version drop-down in the Scalar UI at `/scalar/v1` |
| **CORS** | Fully configurable allowed origins, methods, headers, and credentials |
| **Global Exception Handling** | Middleware converts all unhandled exceptions to structured JSON responses |
| **Docker** | Multi-stage `Dockerfile` — tests must pass before a deployable image is produced |

---

## Architecture

The solution follows **Clean Architecture** with strict dependency direction:

```
API  →  Application  →  Domain
           ↑
     Infrastructure
```

| Layer | Responsibility |
|---|---|
| **Domain** | Core entities, value objects (`CurrencyCode`, `Money`), domain events, interfaces (`IExchangeRateProvider`) |
| **Application** | CQRS queries/handlers (MediatR), FluentValidation validators, MediatR pipeline behaviors, result abstraction |
| **Infrastructure** | `FrankfurterService` HTTP client, `ExchangeRateProviderFactory`, JWT configuration, CORS, Polly resilience |
| **Contracts** | Shared request/response DTOs consumed by the API layer and external clients |
| **API** | Controllers, middleware, rate limiting, OpenAPI, Serilog/OpenTelemetry wiring |

### MediatR Pipeline (innermost → outermost)

```
CachingBehavior → Handler
    ↑
AuthorizationBehavior
    ↑
ValidationBehavior
    ↑
LoggingBehavior
```

---

## Project Structure

```
CurrencyConverter/
├── src/
│   ├── CurrencyConverter.Api/            # Entry point, controllers, middleware
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs         # POST /api/v{n}/auth/token
│   │   │   ├── CurrenciesController.cs   # GET  /api/v{n}/currencies
│   │   │   └── ExchangeRatesController.cs
│   │   ├── Configuration/                # RateLimitingOptions
│   │   ├── Extensions/                   # Serilog, OpenTelemetry, rate limiting, OpenAPI
│   │   └── Middleware/                   # GlobalExceptionHandlingMiddleware
│   ├── CurrencyConverter.Application/    # CQRS handlers, validators, behaviors
│   ├── CurrencyConverter.Infrastructure/ # HTTP clients, JWT, CORS, provider factory
│   ├── CurrencyConverter.Domain/         # Entities, value objects, interfaces
│   └── CurrencyConverter.Contracts/      # Request/response DTOs
├── tests/
│   ├── CurrencyConverter.UnitTests/      # xUnit unit tests
│   └── CurrencyConverter.IntegrationTests/ # WebApplicationFactory integration tests
├── Dockerfile                            # Multi-stage: build → test → publish → runtime
└── Dockerfile.tests                      # Standalone test runner image
```

---

## API Endpoints

All endpoints except `POST /api/v1/auth/token` require a valid **JWT Bearer** token.

### Auth

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/v1/auth/token` | Issues a JWT. Only enabled when `JwtSettings:TestUsername` is set. |

**Request body:**
```json
{ "username": "testuser", "password": "Test@1234" }
```

**Response:**
```json
{ "token": "<jwt>", "expiresIn": 86400 }
```

---

### Currencies

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/currencies` | Returns all currencies supported by Frankfurter. |

---

### Exchange Rates

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/v1/exchangerates/conversion` | Latest rates from a base currency to one or many targets. |
| `POST` | `/api/v1/exchangerates/by-date` | Historical rates for a specific date. |
| `POST` | `/api/v1/exchangerates/amount-conversion` | Converts an amount from one currency to one or more targets. |
| `POST` | `/api/v1/exchangerates/history` | Historical rates over a date range with pagination. |

#### `POST /api/v1/exchangerates/conversion`
```json
{ "baseCurrency": "USD", "targetCurrencies": ["EUR", "GBP"] }
```

#### `POST /api/v1/exchangerates/by-date`
```json
{ "date": "2024-01-15", "baseCurrency": "USD", "targetCurrencies": ["EUR"] }
```

#### `POST /api/v1/exchangerates/amount-conversion`
```json
{ "amount": 100.00, "fromCurrency": "USD", "toCurrencies": ["EUR", "GBP", "JPY"] }
```

#### `POST /api/v1/exchangerates/history`
```json
{
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "baseCurrency": "USD",
  "targetCurrencies": ["EUR"],
  "page": 1,
  "pageSize": 20
}
```

> **Note:** TRY, MXN, and PLN are excluded from conversions per Frankfurter API restrictions. Requests containing these currencies return `422 Unprocessable Entity`.

---

### Health Checks

| Path | Description |
|---|---|
| `/health` | Liveness check — returns `200 Healthy` when the API process is running. |
| `/health/ready` | Readiness check — aggregates all registered health check probes. |

### API Docs

| Path | Description |
|---|---|
| `/scalar/v1` | Interactive Scalar UI (available in Development and Production). |
| `/openapi/v1.json` | Raw OpenAPI JSON document. |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (optional)

### Run Locally

```powershell
# Clone the repository
git clone https://github.com/rsanjar/CurrencyConverter.git
cd CurrencyConverter

# Restore and run
dotnet restore
dotnet run --project src/CurrencyConverter.Api/CurrencyConverter.Api.csproj
```

The API listens on `https://localhost:7xxx` / `http://localhost:5xxx` (ports shown on startup).

To enable the test token endpoint in development, ensure `appsettings.Development.json` contains:

```json
"JwtSettings": {
  "TestUsername": "testuser",
  "TestPassword": "Test@1234"
}
```

### Run with Docker

The `Dockerfile` runs the full test suite during the build. The final image is only produced when all tests pass.

```bash
# Build (runs unit + integration tests internally)
docker build -t currency-converter .

# Run
docker run -p 8080:8080 currency-converter
```

---

## Configuration Reference

All settings live in `appsettings.json` and can be overridden per environment via `appsettings.{Environment}.json`, environment variables, or .NET user secrets.

---

### Serilog

Configures structured logging. The section is read directly by the `Serilog.Settings.Configuration` package.

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Grafana.Loki" ],
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning"
    }
  },
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
      }
    }
  ],
  "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithProcessId" ]
}
```

| Key | Description |
|---|---|
| `Using` | Serilog sink assemblies to load. Add `"Serilog.Sinks.Grafana.Loki"` to push logs to Loki. |
| `MinimumLevel.Default` | Global minimum log level (`Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`). |
| `MinimumLevel.Override` | Per-namespace overrides to silence verbose framework logs. |
| `WriteTo` | List of sinks. Each entry uses the sink's `Name` and optional `Args`. Add a `GrafanaLoki` entry with `"uri"` to enable Loki. |
| `Enrich` | Enrichers that attach extra properties to every log event. |

---

### OpenTelemetry

Used by the (currently commented-out) `builder.AddOpenTelemetry()` call in `Program.cs`. Uncomment to enable distributed tracing and Prometheus metrics.

```json
"OpenTelemetry": {
  "ServiceName": "currency-converter-svc",
  "ServiceVersion": "1.0.0",
  "OtlpEndpoint": "http://tempo:4317"
}
```

| Key | Description |
|---|---|
| `ServiceName` | Service name tag attached to all traces and metrics. |
| `ServiceVersion` | Service version tag. |
| `OtlpEndpoint` | OTLP gRPC endpoint for Grafana Tempo (or any OTLP-compatible backend). Use `http://localhost:4317` locally. |

When enabled, Prometheus metrics are exposed at `/metrics` via `app.MapPrometheusScrapingEndpoint()`.

---

### ExchangeRateProvider

Selects the active exchange-rate data source.

```json
"ExchangeRateProvider": {
  "DefaultProvider": "Frankfurter"
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `DefaultProvider` | `string` (enum) | `Frankfurter` | The provider used when none is specified per-request. Currently only `Frankfurter` is implemented. Adding a new provider requires implementing `IExchangeRateProvider` and registering it in `ExchangeRateProviderFactory`. |

---

### Frankfurter

HTTP client settings for the [Frankfurter API](https://www.frankfurter.app/docs/), including resilience policy parameters.

```json
"Frankfurter": {
  "BaseUrl": "https://api.frankfurter.app",
  "TimeoutSeconds": 60,
  "RetryMaxAttempts": 3,
  "RetryBaseDelaySeconds": 1.0,
  "CircuitBreakerFailureRatio": 0.5,
  "CircuitBreakerSamplingDurationSeconds": 30,
  "CircuitBreakerBreakDurationSeconds": 30,
  "CircuitBreakerMinimumThroughput": 10
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `BaseUrl` | `string` | `https://api.frankfurter.app` | Base URL of the Frankfurter API. Override to point at a self-hosted instance. |
| `TimeoutSeconds` | `int` | `90` | **Total** request timeout in seconds, covering all retry attempts combined. Should be higher than a single-attempt timeout. |
| `RetryMaxAttempts` | `int` | `3` | Maximum number of retry attempts on transient HTTP failures. |
| `RetryBaseDelaySeconds` | `double` | `1.0` | Base delay in seconds for exponential back-off. Actual delay: `BaseDelay × 2^attempt ± jitter`. |
| `CircuitBreakerFailureRatio` | `double` | `0.5` | Fraction of failures (0–1) within the sampling window that trips the circuit breaker. |
| `CircuitBreakerSamplingDurationSeconds` | `int` | `30` | Duration of the sliding window used to calculate the failure ratio. |
| `CircuitBreakerBreakDurationSeconds` | `int` | `30` | Duration the circuit stays open (rejecting all requests) before allowing a single probe request. |
| `CircuitBreakerMinimumThroughput` | `int` | `10` | Minimum number of requests required within the sampling window before the circuit can trip. |

---

### JwtSettings

Controls JWT bearer token validation and issuance. Supports both **symmetric** (HMAC-SHA256) and **asymmetric** (RSA) modes.

```json
"JwtSettings": {
  "SecretKey": "change-this-to-a-strong-secret-key-at-least-32-chars-and-save-in-secrets",
  "PublicKey": "",
  "Issuer": "CurrencyConverter",
  "Audience": "CurrencyConverter",
  "ExpirationMinutes": 60,
  "TestUsername": "",
  "TestPassword": ""
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `SecretKey` | `string` | *(required)* | HMAC-SHA256 symmetric secret. Must be **at least 32 characters**. Used when `PublicKey` is empty. **Never commit the production value — use environment variables or .NET user secrets.** |
| `PublicKey` | `string` | `""` | RSA public key in PEM format (`-----BEGIN PUBLIC KEY-----...`). When non-empty, asymmetric RSA validation is used instead of HMAC. |
| `Issuer` | `string` | `CurrencyConverter` | Expected `iss` claim in incoming tokens. Validation is skipped when this value is empty. |
| `Audience` | `string` | `CurrencyConverter` | Expected `aud` claim in incoming tokens. Validation is skipped when this value is empty. |
| `ExpirationMinutes` | `int` | `60` | Lifetime in minutes of tokens issued by `POST /api/v1/auth/token`. |
| `TestUsername` | `string` | `""` | Username accepted by the test token endpoint. **Leave empty in production** to disable the endpoint entirely. |
| `TestPassword` | `string` | `""` | Password accepted by the test token endpoint. **Leave empty in production.** |

---

### RateLimiting

All three policies use a **fixed-window** algorithm. Rejected requests receive `429 Too Many Requests` with a `Retry-After` header.

```json
"RateLimiting": {
  "Enabled": true,
  "Authenticated": {
    "PermitLimit": 60,
    "WindowSeconds": 60,
    "QueueLimit": 0
  },
  "Auth": {
    "PermitLimit": 10,
    "WindowSeconds": 60,
    "QueueLimit": 0
  },
  "GlobalIp": {
    "Enabled": true,
    "PermitLimit": 200,
    "WindowSeconds": 60,
    "QueueLimit": 0
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Master switch. Set to `false` to disable all rate limiting (useful in tests). |
| **`Authenticated`** | object | — | Fixed-window policy for authenticated API endpoints, partitioned **per username** extracted from the JWT. |
| **`Auth`** | object | — | Fixed-window policy for `POST /api/v{n}/auth/token`, partitioned **per client IP** to prevent credential brute-forcing. |
| **`GlobalIp`** | object | — | Global ceiling applied to **every** request, partitioned per client IP. Evaluated before any named policy. |

Each policy object (`Authenticated`, `Auth`, `GlobalIp`) shares the same fields:

| Field | Type | Description |
|---|---|---|
| `Enabled` | `bool` | Whether this specific policy is active (`GlobalIp` only; the others inherit the master `Enabled` flag). |
| `PermitLimit` | `int` | Maximum requests allowed within the window. |
| `WindowSeconds` | `int` | Length of the fixed window in seconds. |
| `QueueLimit` | `int` | Requests to queue when the limit is hit before rejecting. `0` = reject immediately. |

---

### Cors

```json
"Cors": {
  "PolicyName": "DefaultPolicy",
  "AllowedOrigins": [],
  "AllowedMethods": [ "GET", "POST", "PUT", "DELETE", "OPTIONS" ],
  "AllowedHeaders": [ "Content-Type", "Authorization" ],
  "AllowCredentials": false
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `PolicyName` | `string` | `DefaultPolicy` | Internal name of the CORS policy registered and applied by the middleware. |
| `AllowedOrigins` | `string[]` | `[]` | Allowed origins (e.g. `["https://myapp.com"]`). An **empty array** allows any origin — use only in development. |
| `AllowedMethods` | `string[]` | `["GET","POST","PUT","DELETE","OPTIONS"]` | HTTP methods the browser is permitted to use in cross-origin requests. |
| `AllowedHeaders` | `string[]` | `["Content-Type","Authorization"]` | Request headers allowed in cross-origin requests. |
| `AllowCredentials` | `bool` | `false` | Allows cookies/auth headers in cross-origin requests. Only takes effect when `AllowedOrigins` is non-empty. |

---

### TheTechLoopCache

Configures `TheTechLoop.HybridCache`, a first-party caching library that integrates with the MediatR pipeline. Queries that implement `ICacheable` are automatically cached.

```json
"TheTechLoopCache": {
  "UseMemoryOnly": true,
  "Enabled": true,
  "ServiceName": "currency-converter",
  "MemoryCache": {
    "SizeLimit": 1024
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Master switch for all caching. Set to `false` to bypass the cache entirely. |
| `UseMemoryOnly` | `bool` | `true` | `true` — use an in-process `IMemoryCache` only. `false` — use a hybrid strategy (memory L1 + Redis L2). When using Redis, configure the Redis connection string separately. |
| `ServiceName` | `string` | `currency-converter` | Prefix added to all cache keys to avoid collisions when multiple services share a Redis instance. |
| `MemoryCache.SizeLimit` | `int` | `1024` | Maximum number of entries the in-process memory cache may hold before eviction. |

**Cache durations per query:**

| Query | Duration |
|---|---|
| `GetLatestRatesQuery` | 5 minutes |
| `ConvertCurrencyQuery` | 5 minutes |
| `GetHistoricalRatesQuery` | 24 hours |
| `GetHistoricalRatesRangeQuery` | 24 hours |
| `GetAvailableCurrenciesQuery` | 24 hours |

---

## API Versioning

All routes are prefixed with a URL version segment: `/api/v{n}/...`. The current version is **v1**.

### Strategy

| Aspect | Detail |
|---|---|
| **Style** | URL-segment — explicit, cache-friendly, and trivially routable at the reverse-proxy level |
| **Default version** | `1.0` — requests that omit the version segment are treated as v1 |
| **Version header** | Every response includes `api-supported-versions: 1.0` (`ReportApiVersions = true`) |
| **OpenAPI docs** | One document per version at `/openapi/v{n}.json`; each appears in the Scalar drop-down |
| **Scalar UI** | Version drop-down at the top — selecting a version loads its document and pre-fills all URLs with the correct version, so no manual entry is needed |

### NuGet packages

| Package | Role |
|---|---|
| `Asp.Versioning.Http` | Core versioning for ASP.NET Core endpoint routing |
| `Asp.Versioning.Mvc` | Wires versioning into the MVC controller pipeline |
| `Asp.Versioning.Mvc.ApiExplorer` | Provides `SubstituteApiVersionInUrl` — replaces `{version:apiVersion}` with the concrete value in the OpenAPI spec |

### Controller annotation

Every controller is decorated with `[ApiVersion("1.0")]` and uses the shared route prefix:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
```

### Adding v2

Only two lines in `Program.cs` need to change:

```csharp
// Before
builder.Services.AddCustomOpenApi("Currency Converter API", ["v1"]);
app.MapCustomOpenApi("Currency Converter API", ["v1"]);

// After
builder.Services.AddCustomOpenApi("Currency Converter API", ["v1", "v2"]);
app.MapCustomOpenApi("Currency Converter API", ["v1", "v2"]);
```

Then create a new controller with `[ApiVersion("2.0")]`. The v1 controllers remain unchanged.

---

## Authentication

The API uses **JWT Bearer** authentication. Obtain a token from `POST /api/v1/auth/token` (development only) and pass it in the `Authorization` header:

```
Authorization: Bearer <token>
```

For production deployments, issue tokens from your own identity provider and configure `JwtSettings:Issuer`, `JwtSettings:Audience`, and either `JwtSettings:SecretKey` (symmetric) or `JwtSettings:PublicKey` (asymmetric RSA PEM) to match.

---

## Rate Limiting

Three fixed-window policies are active by default:

| Policy | Partition | Limit | Window |
|---|---|---|---|
| `GlobalIp` | Client IP | 200 req | 60 s |
| `Authenticated` | JWT username | 60 req | 60 s |
| `Auth` | Client IP | 10 req | 60 s |

All limits and windows are fully configurable via the [`RateLimiting`](#ratelimiting) section in `appsettings.json`. Rejected requests receive `429 Too Many Requests` with a `Retry-After` header indicating when to retry.

---

## Caching

All MediatR queries that implement `ICacheable` are transparently cached by the `CachingBehavior` pipeline behavior. No changes to handlers or controllers are needed. Cache keys are deterministic (sorted target currencies) so equivalent queries with different orderings hit the same entry.

By default the API uses an **in-process memory cache**. Set `TheTechLoopCache:UseMemoryOnly` to `false` and configure a Redis connection string to switch to hybrid (L1 memory + L2 Redis) mode.

---

## Resilience

The `FrankfurterService` HTTP client is wrapped with Polly's **standard resilience handler**, configured via the [`Frankfurter`](#frankfurter) section:

- **Retry**: Up to 3 attempts with exponential back-off (base 1 s) and jitter.
- **Circuit Breaker**: Trips when ≥ 50 % of requests fail within a 30-second window (minimum 10 requests). Stays open for 30 seconds before a probe is allowed.
- **Total Timeout**: 60 seconds covering all attempts combined; prevents `HttpClient` from interfering with the pipeline.

When the circuit is open, `POST` requests to Frankfurter fail immediately with `502 Bad Gateway`.

---

## Observability

### Structured Logging (Serilog)

All log events are enriched with `MachineName`, `EnvironmentName`, `ThreadId`, and `ProcessId`. The `LoggingBehavior` MediatR behavior logs every request name, duration, and outcome. Requests slower than **500 ms** are logged at `Warning`.

To push logs to Grafana Loki, add the `Serilog.Sinks.Grafana.Loki` sink to the `WriteTo` array in `appsettings.json`.

### Distributed Tracing & Metrics (OpenTelemetry)

Uncomment `builder.AddOpenTelemetry()` in `Program.cs` to enable:

- **Traces**: ASP.NET Core and `HttpClient` instrumentation exported to Grafana Tempo via OTLP gRPC.
- **Metrics**: ASP.NET Core, `HttpClient`, and .NET runtime metrics exposed on `/metrics` for Prometheus.

Configure the OTLP endpoint via `OpenTelemetry:OtlpEndpoint`.

---

## Error Handling

The `GlobalExceptionHandlingMiddleware` converts all unhandled exceptions to structured JSON:

```json
{ "error": "<message>", "errors": [] }
```

| Exception | HTTP Status |
|---|---|
| `ValidationException` (FluentValidation) | `422 Unprocessable Entity` |
| `UnauthorizedException` | `401 Unauthorized` |
| `ForbiddenAccessException` | `403 Forbidden` |
| `HttpRequestException` (upstream failure) | `502 Bad Gateway` |
| `OperationCanceledException` | `499 Client Closed Request` |
| Any other exception | `500 Internal Server Error` |

---

## Testing

The solution contains two test projects:

| Project | Type | Tools |
|---|---|---|
| `CurrencyConverter.UnitTests` | Unit | xUnit, Moq, FluentAssertions |
| `CurrencyConverter.IntegrationTests` | Integration | xUnit, `WebApplicationFactory<Program>` |

```powershell
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/CurrencyConverter.UnitTests/

# Run only integration tests
dotnet test tests/CurrencyConverter.IntegrationTests/
```

The multi-stage `Dockerfile` runs both test suites during the Docker build. The final runtime image is only produced when all tests pass, ensuring broken builds are never deployed.
