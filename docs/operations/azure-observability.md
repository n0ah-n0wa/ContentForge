# Azure Observability (Application Insights)

ContentForge exports production telemetry to **Azure Application Insights** using the official OpenTelemetry distro (`Azure.Monitor.OpenTelemetry.AspNetCore`).

## What is captured

| Signal | Source | Notes |
|--------|--------|-------|
| **HTTP requests** | ASP.NET Core instrumentation | Duration, status code, route; health probes excluded |
| **Exceptions** | ASP.NET Core + centralized exception handler | Stack traces; sensitive message text redacted |
| **Dependencies** | EF Core + HttpClient instrumentation | DB and outbound HTTP calls; SQL text **not** captured |
| **Performance metrics** | ASP.NET Core, HttpClient, .NET runtime | Request rates, latency, GC/thread-pool metrics |
| **Structured logs** | `ILogger` via OpenTelemetry log export | JSON console logging remains enabled locally |

Custom dimensions on requests:

- `correlationId` — from `X-Correlation-ID` middleware
- `userId` — JWT `sub` claim when authenticated (not email or display name)

## Environment behavior

| Environment | Application Insights |
|-------------|---------------------|
| **Testing** | Always disabled (CI and integration tests never export telemetry) |
| **Development** | Disabled by default; opt in with `ApplicationInsights:Enabled=true` and a connection string |
| **Staging** | Enabled automatically when a connection string is configured |
| **Production** | Enabled automatically when a connection string is configured |

Structured JSON console logging is always enabled regardless of Application Insights.

## Configuration

### Recommended (Azure App Service)

Set an application setting (or Key Vault reference):

```text
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...;IngestionEndpoint=...;LiveEndpoint=...
```

Optional tuning via appsettings or App Service settings:

```json
{
  "ApplicationInsights": {
    "Enabled": true,
    "ConnectionString": "",
    "SamplingRatio": 0.25
  }
}
```

When both `ApplicationInsights:ConnectionString` and `APPLICATIONINSIGHTS_CONNECTION_STRING` are set, the appsettings value takes precedence.

### Local development opt-in

To send telemetry from a developer machine (not recommended for routine work):

1. Create an Application Insights resource in Azure.
2. Copy the connection string.
3. Set user secrets or environment variables:

```bash
dotnet user-secrets set "ApplicationInsights:Enabled" "true"
dotnet user-secrets set "ApplicationInsights:ConnectionString" "<connection-string>"
```

Or:

```bash
set APPLICATIONINSIGHTS_CONNECTION_STRING=<connection-string>
set ApplicationInsights__Enabled=true
```

Leave `Enabled=false` (default) for normal local development.

### Sampling

`SamplingRatio` accepts values from `0` to `1`. Use `1.0` in Staging and lower values (for example `0.1`–`0.25`) in high-traffic Production to control cost.

## Sensitive data protection

Telemetry must not contain passwords, tokens, secrets, or unnecessary personal data. ContentForge applies multiple safeguards:

1. **Source logging** — request middleware never logs bodies or `Authorization` headers; exception logging uses `SensitiveLogRedactor`.
2. **Instrumentation** — EF Core SQL statement capture is disabled; health endpoints are excluded from request traces.
3. **Export processors** — OpenTelemetry activity and log processors redact known sensitive tags and attributes before export.

Redacted values appear as `[REDACTED]` in exported telemetry.

## Health probes

`/health/live` and `/health/ready` are filtered out of HTTP request traces to avoid noise from App Service and Kubernetes probes. Health endpoints continue to return correlation IDs and remain available without authentication.

## App Service integration

1. Create an Application Insights workspace-linked component.
2. Enable **Application Insights** on the App Service (or link an existing component).
3. Confirm `APPLICATIONINSIGHTS_CONNECTION_STRING` is injected automatically.
4. Configure the **Health check path** to `/health/ready` under App Service → Health check.
5. Add alerts for:
   - failed readiness checks;
   - elevated server exception rate;
   - dependency failure spikes.

## Verification

### Local (Development, telemetry off)

```bash
dotnet run --project src/ContentForge.Api
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

The API starts and health endpoints respond without requiring a connection string.

### Automated tests

Integration tests run under the `Testing` environment; Application Insights registration is skipped entirely:

```bash
dotnet test -- xUnit.MaxParallelThreads=1
```

### Staging smoke test

After deployment, open Application Insights → **Transaction search** or **Logs** and confirm:

- incoming HTTP requests appear for API routes;
- dependency calls show database and storage activity;
- exceptions appear for forced error paths in a controlled test;
- `correlationId` is present on request telemetry.

## Related documentation

- [ARCHITECTURE.md](../ARCHITECTURE.md) — observability overview
- [DEVELOPMENT_RULES.md](../DEVELOPMENT_RULES.md) — logging conventions
- [azure-storage.md](./azure-storage.md) — Blob Storage configuration
