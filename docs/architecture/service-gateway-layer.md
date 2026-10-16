# Service Gateway Layer

> For decision rationale, see [ADR-005](../decisions/architecture/005-service-gateway-layer.md)
> (gateway layer) and [ADR-010](../decisions/architecture/010-hand-written-servarr-api-contracts.md)
> (hand-written Refit contracts). For the Custom Format passthrough exception, see
> [ADR-007](../decisions/architecture/007-custom-format-gateway-passthrough.md).

## Overview

The service gateway layer sits between sync operations and HTTP clients, abstracting
service-specific API details behind domain interfaces. Each feature gets a **port** (domain
interface) and one **gateway per service** (infrastructure implementation).

Sync operations inject the port and operate on domain types. They have no knowledge of which service
is active or how HTTP requests are made.

## Port + Gateway Pattern

### Port

Domain interface defined in `src/Recyclarr.Core/Servarr/{Feature}/`. Speaks domain types only.
Methods use read-only collection types (`IReadOnlyList<T>`) for both parameters and return values.

Ports are not sync-exclusive. System status is consumed by the compatibility layer, not a sync
operation.

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
    ISonarrNamingService.cs          # no shared fields: one port per service
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
while preserving lossless round-trips. Service DTOs keep fields they do not model in their own
extension data, so the stashed DTO carries them back on update.

```csharp
internal class SonarrV3QualityDefinitionGateway(IQualityDefinitionApiService api)
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

**When not needed:** Operations with one-way sync (guide overwrites service) do not need stashed
DTOs. See the Custom Format passthrough exception below.

## DI Resolution

Gateways are keyed by `ServiceApiTarget` (`SonarrV3`, `SonarrV5`, `RadarrV3`), the API generation a
service instance speaks. A non-keyed registration resolves the port by the detected target:

```csharp
builder
    .RegisterServiceGateway<IQualityProfileService>()
    .For<SonarrV3QualityProfileGateway>(ServiceApiTarget.SonarrV3)
    .For<SonarrV5QualityProfileGateway>(ServiceApiTarget.SonarrV5)
    .For<RadarrQualityProfileGateway>(ServiceApiTarget.RadarrV3);
```

The target comes from `DetectedService`, produced by the capability check from the server version
(see [ADR-028](../decisions/architecture/028-api-version-selection-by-server-major.md)).
`InstanceSyncProcessor` and the server's `InstanceServiceCaller` run the check, then begin a child
scope with `BeginDetectedServiceScope` that registers `DetectedService`; planning, sync operations,
and instance calls resolve from that scope. Resolving a port before detection fails with
`DependencyResolutionException` instead of defaulting. `ISystemService` is the exception: it
performs detection, so it stays keyed by `SupportedServices`.

A sync operation whose port has no gateway on some target resolves it lazily and is skipped by its
plan there (the quality size operation on Sonarr v5).

Sync operations and per-service components inject the port directly; they have no idea keying is
involved.

## One operation per pipeline

[ADR-023][adr-023] sets the rule: each field lives on a shared type or on the type of the one
service that owns it, and each pipeline has one operation with per-service components resolved
through DI with `RegisterServiceGateway`. Quality profiles (`IQualityProfileServiceFields`) and
media naming (`IMediaNamingPlanner`, `IMediaNamingSync`) follow it. Components are keyed by API
target, so a field one API generation owns gets its own component (Sonarr v5 per-profile quality
sizes in `SonarrV5QualityProfileFields`).

Shared logic lives in helper classes that both services' components depend on (e.g.
`NamingFormatLookup`).

## Gateway multiplicity

Every pipeline gets one gateway per API target it applies to, regardless of whether its port is
shared. Sonarr has one gateway per API generation (`SonarrV3*`, `SonarrV5*`).

- **Shared ports:** All gateways implement the same port. DI resolves the correct one by target.
- **Per-service ports:** Each service's component injects its own port, with one gateway per target
  of that service (e.g. `ISonarrNamingService` implemented by `SonarrV3NamingGateway` and
  `SonarrV5NamingGateway`).
- **Target-specific ports:** `IQualityDefinitionService` has no Sonarr v5 gateway; v5 stores sizes
  on quality profile items, so the quality profile gateway owns them.

There is never a single "shared" gateway that handles both services. Even when schemas are identical
(e.g. custom formats, system status), separate gateways per service are required because each
service backs a different API client.

## Domain Type Design

### Required properties

Driven by domain invariants, not API schema. A property is `required` only when omitting it at
construction would silently mask a bug in domain logic.

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

Custom Format gateways follow the standard DTO/domain split pattern. The Refit DTOs serve as service
types; gateways map to/from the domain `CustomFormatResource`. One-way sync still holds (guide is
complete source of truth), so no stashed DTOs or hydrated resource pattern is needed.

Previously, CF gateways used a passthrough exception (see [ADR-007][adr-007], now deprecated) where
`CustomFormatResource` served directly as both guide and API type. The Refit migration introduced
service DTOs as separate C# types, breaking the identical-schema condition that justified the
exception.

## HTTP Pipeline

Gateways use hand-written Refit interfaces backed by `IHttpClientFactory` named clients.
Cross-cutting concerns live in `DelegatingHandler` subclasses, configured once per named client.

### Named clients

| Client      | Handlers             | Primary handler config   |
| ----------- | -------------------- | ------------------------ |
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

Comparison of Sonarr and Radarr APIs for endpoints Recyclarr consumes. Useful when deciding which
fields are shared and which are service-owned for new features.

| Endpoint            | Divergence           | Notes                                                  |
| ------------------- | -------------------- | ------------------------------------------------------ |
| Custom Formats      | Identical            | Truly shareable                                        |
| Quality Definitions | Identical            | Nested `Quality` model differs but main schema matches |
| Language            | Identical            |                                                        |
| System Status       | Minimal              | `sqliteVersion` Sonarr-only                            |
| Quality Profiles    | Divergent            | Radarr adds `language` field                           |
| Media Management    | Divergent            | Domain-specific fields, different enum values          |
| Media Naming        | Completely different | Almost no overlap beyond `id`                          |

Both services publish OpenAPI specs in their repositories; use them as a reference when adding
fields or endpoints.

[adr-007]: ../decisions/architecture/007-custom-format-gateway-passthrough.md
[adr-023]: ../decisions/architecture/023-service-owned-fields-in-shared-pipelines.md
