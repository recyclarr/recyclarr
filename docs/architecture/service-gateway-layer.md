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

- Owns the HTTP client (Flurl today, Refit after migration)
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

**Lifecycle:** Gateway instances are scoped to one sync operation (`InstancePerLifetimeScope`).
State is created during fetch, consumed during persist, and discarded when the sync completes.

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
today (e.g. custom formats, system status), separate gateways per service are required because they
will back different generated Refit clients after the Refit migration.

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

## Custom Format Passthrough Exception

Custom Formats are an exception to the DTO/domain split pattern. The gateway uses
`CustomFormatResource` (the guide resource type) directly as the port's domain type, with no
separate service DTO and no stashed state. See [ADR-007][adr-007] for full rationale.

**Why CF qualifies:**

- CF sync is one-way push (guide is complete source of truth; no fetch-modify-push)
- Guide JSON format matches the API format (identical schemas); the one divergence (`fields` as
  object vs array) is handled by `FieldsArrayJsonConverter` at the serialization level
- Complex nested type graph (3 types with custom JSON converters) makes duplication expensive
- The CF -> QP pipeline dependency relies on in-place mutation of `PlannedCustomFormat.Resource.Id`

**General passthrough criteria** (all must hold):

1. One-way sync model (guide/config defines complete desired state)
2. Identical or trivially-bridged schemas (format differences handled by serialization, not mapping)
3. Complex nested type graph where duplication is expensive and error-prone

When any condition is absent, the standard DTO/domain split applies.

[adr-007]: ../decisions/architecture/007-custom-format-gateway-passthrough.md
