# Service Gateway Layer

> For decision rationale, see [ADR-005](../decisions/architecture/005-service-gateway-layer.md)
> (gateway layer) and [ADR-006](../decisions/architecture/006-refit-refitter-api-clients.md)
> (Refit/Refitter migration). For the Custom Format passthrough exception, see
> [ADR-007](../decisions/architecture/007-custom-format-gateway-passthrough.md).

## Overview

The service gateway layer sits between pipeline phases and HTTP clients, abstracting
service-specific API details behind domain interfaces. Each feature gets a **port** (domain
interface) and one **gateway per service** (infrastructure implementation).

Pipeline phases inject the port and operate on domain types. They have no knowledge of which service
is active or how HTTP requests are made.

## Port + Gateway Pattern

### Port

Domain interface defined in `src/Recyclarr.Core/Servarr/{Feature}/`. Speaks domain types only.
Methods use read-only collection types (`IReadOnlyList<T>`) for both parameters and return values.

Ports are not pipeline-exclusive. System status is consumed by the compatibility layer, not a
pipeline.

```txt
Servarr/
  QualitySize/
    IQualityDefinitionService.cs
    QualityDefinitionItem.cs
  SystemStatus/
    ISystemService.cs
    SystemServiceResult.cs
  CustomFormat/
    ICustomFormatService.cs          # uses CustomFormatResource (passthrough)
  MediaManagement/
    IMediaManagementService.cs
    MediaManagementData.cs
  MediaNaming/
    ISonarrNamingService.cs          # split pipeline: one port per service
    IRadarrNamingService.cs
    SonarrNamingData.cs
    RadarrNamingData.cs
```

### Gateway

Infrastructure class in `src/Recyclarr.Core/ServarrApi/{Feature}/` implementing the port for a
specific service. Responsibilities:

- Owns the HTTP client
- Contains to/from-domain mapping as **private methods** (no separate translator class)
- Manages read-modify-write state via the hydrated resource pattern (when applicable)

Named `*Gateway` (not `*Adapter`) to avoid confusion with the GoF Adapter pattern.

### Service DTO conventions

DTO types in `ServarrApi/{Feature}/` use the `Service` prefix to avoid collision with domain types
and namespace names:

- `ServiceQualityDefinitionItem` (DTO) vs `QualityDefinitionItem` (domain)
- `ServiceSystemStatus` (DTO) vs `SystemServiceResult` (domain)
- `ServiceMediaManagementData` (DTO) vs `MediaManagementData` (domain)

### Why no separate translator classes

The mapping logic is private to one gateway, called from one place, and tested through the gateway's
public surface. Extracting it to a separate class adds a file and indirection with no benefit to
testability, reuse, or readability. Even for complex cases (nested DTO graphs, many input shape
variations), the test setup cost of going through the gateway is negligible.

## Hydrated Resource Pattern

Gateways that perform read-modify-write operations hold the original service DTO in an internal
dictionary keyed by resource ID. This eliminates the need for `[JsonExtensionData]` on domain types
while preserving lossless round-trips.

```csharp
internal class SonarrQualityDefinitionGateway(IQualityDefinitionApiService api)
    : IQualityDefinitionService
{
    private readonly Dictionary<int, ServiceQualityDefinitionItem> _stashedDtos = [];

    public async Task<IReadOnlyList<QualityDefinitionItem>> GetQualityDefinitions(
        CancellationToken ct)
    {
        var dtos = await api.GetQualityDefinition(ct);
        foreach (var dto in dtos)
        {
            _stashedDtos[dto.Id] = dto;
        }
        return dtos.Select(ToDomain).ToList();
    }

    public async Task UpdateQualityDefinitions(
        IReadOnlyList<QualityDefinitionItem> items, CancellationToken ct)
    {
        // Merge domain changes onto stashed DTOs for round-trip safety
        var apiItems = items.Select(FromDomain).ToList();
        await api.UpdateQualityDefinition(apiItems, ct);
    }
}
```

**Lifecycle:** Gateway instances are scoped to one sync instance (`InstancePerLifetimeScope`). State
is created during fetch, consumed during persist, and discarded when the instance scope ends.

**When not needed:** Pipelines with one-way sync (guide overwrites service) do not need stashed
DTOs. See the Custom Format passthrough exception below.

## DI Resolution

Keyed registration per service, plus a non-keyed lambda that selects the correct gateway based on
`IServiceConfiguration.ServiceType` at resolve time:

```csharp
// Shared pipeline: keyed + non-keyed resolution
builder.RegisterServiceGateway<
    IQualityDefinitionService,
    SonarrQualityDefinitionGateway,
    RadarrQualityDefinitionGateway>();

// Split pipeline: direct registration (one gateway per port)
builder.RegisterType<SonarrNamingGateway>()
    .As<ISonarrNamingService>()
    .InstancePerLifetimeScope();
```

Pipeline phases inject the port directly; they have no idea keying is involved.

## Shared vs Split Pipeline

**Litmus test:** Is there a meaningful shared domain concept, or just a shared endpoint path?

- **Shared pipeline + gateway:** Same resource with service-specific properties (e.g. quality
  profiles with Radarr language).
- **Split into service-specific pipelines:** Different resource concepts behind a shared path (e.g.
  media naming).

Split pipelines don't mean "duplicate everything." Shared logic lives in helper classes that both
pipelines depend on (e.g. `NamingFormatLookup`).

## Gateway Multiplicity

Every pipeline always gets one gateway per service (Sonarr + Radarr), regardless of whether the
pipeline is shared or split.

- **Shared pipelines:** Both gateways implement the same port. DI resolves the correct one via
  keyed/non-keyed pattern.
- **Split pipelines:** Each pipeline has its own port and single gateway (e.g.
  `ISonarrNamingService` implemented by `SonarrNamingGateway`).

There is never a single "shared" gateway that handles both services. Even when schemas are identical
(e.g. custom formats, system status), separate gateways per service are required because each
service backs a different API client.

## Domain Type Design

### Required properties

Driven by domain/pipeline invariants, not API schema. A property is `required` only when omitting it
at construction would silently mask a bug in domain logic.

Decision tree:

1. Can the caller ever reasonably not care about this field's value? If yes, non-required with
   sensible default.
2. Would a default value silently break matching, lookup, or business logic? If yes, `required`.
3. Otherwise, non-required with that safe default.

Examples: `QualityName` is required (empty string breaks matching). `Id` is not (zero is harmless;
the gateway always sets it). Nullable fields like `MaxSize` default to null (domain meaning:
"unlimited").

### Plan phase data flow

Plan components build domain types, never DTOs. The flow is:

1. Plan builds a domain model from user configuration (desired state)
2. Fetch retrieves current state via gateway (DTO to domain)
3. Transaction merges planned state onto fetched state (domain in, domain out)
4. Persist sends merged state via gateway (domain to DTO)

DTOs should only appear inside gateways.

## Custom Format Gateways

Custom Format gateways follow the standard DTO/domain split pattern. The generated Refit DTOs serve
as service types; gateways map to/from the domain `CustomFormatResource`. One-way sync still holds
(guide is complete source of truth), so no stashed DTOs or hydrated resource pattern is needed.

Previously, CF gateways used a passthrough exception (see [ADR-007][adr-007], now deprecated) where
`CustomFormatResource` served directly as both guide and API type. The Refit migration introduced
generated DTOs as separate C# types, breaking the identical-schema condition that justified the
exception.

## HTTP Pipeline

Gateways use Refit-generated interfaces backed by `IHttpClientFactory` named clients. Cross-cutting
concerns live in `DelegatingHandler` subclasses, configured once per named client.

### Named clients

| Client      | Handlers             | Primary handler config   |
|-------------|----------------------|--------------------------|
| `"servarr"` | `HttpLoggingHandler` | SSL bypass (conditional) |
| `"apprise"` | `HttpLoggingHandler` | (default)                |

### Handler pipeline (servarr)

```txt
HttpLoggingHandler -> HttpClientHandler -> network
```

- **HttpLoggingHandler**: Logs request method + sanitized URL (Debug), response status (Debug), and
  request/response bodies (Verbose). Also detects failed requests that were redirected (by comparing
  original vs final URI) and logs a warning about potential URL/reverse proxy misconfiguration.
  Replaces Flurl's `FlurlBeforeCallLogRedactor`, `FlurlAfterCallLogRedactor`, and
  `FlurlRedirectPreventer`.
- **SSL bypass**: Conditionally disables certificate validation via
  `ConfigurePrimaryHttpMessageHandler` based on `EnableSslCertificateValidation` setting.

### Refit client wiring

Per-instance configuration (base URL, API key) is set at Autofac resolve time, not at
`IHttpClientFactory` registration time. Extension methods on `ContainerBuilder` eliminate
boilerplate:

```csharp
// Each call registers a Refit interface scoped to one sync instance
builder.RegisterServarrRefitClient<ISonarrCustomFormatApi>();
builder.RegisterAppriseRefitClient<IAppriseApi>();
```

The resolve lambda calls `IHttpClientFactory.CreateClient("servarr")`, sets `BaseAddress` and
`X-Api-Key` from `IServiceConfiguration`, then wraps the client with `RestService.For<T>()`. Refit
clients are scoped to the "instance" lifetime scope
(`InstancePerMatchingLifetimeScope("instance")`), matching the gateway lifecycle. The underlying
handler pipeline is pooled by `IHttpClientFactory`.

### IHttpClientFactory + Autofac integration

`ServiceCollection` defines the named clients and handler pipeline, then
`builder.Populate(services)` bridges them into Autofac. `Populate()` registers
`AutofacServiceProvider` as `IServiceProvider`, so handler factories (including
`ConfigurePrimaryHttpMessageHandler`) can resolve Autofac services.

## Service API Divergence

Comparison of Sonarr and Radarr OpenAPI specs for endpoints Recyclarr consumes. Useful when deciding
shared vs split pipeline for new features.

| Endpoint            | Divergence           | Notes                                                  |
|---------------------|----------------------|--------------------------------------------------------|
| Custom Formats      | Identical            | Truly shareable                                        |
| Quality Definitions | Identical            | Nested `Quality` model differs but main schema matches |
| Language            | Identical            |                                                        |
| System Status       | Minimal              | `sqliteVersion` Sonarr-only                            |
| Quality Profiles    | Divergent            | Radarr adds `language` field                           |
| Media Management    | Divergent            | Domain-specific fields, different enum values          |
| Media Naming        | Completely different | Almost no overlap beyond `id`                          |

Both services publish official OpenAPI 3.0 specs (auto-generated via Swashbuckle).

[adr-007]: ../decisions/architecture/007-custom-format-gateway-passthrough.md
