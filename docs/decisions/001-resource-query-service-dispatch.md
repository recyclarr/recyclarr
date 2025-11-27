# ADR-001: Resource Query Service Dispatch Pattern

## Status

Accepted

## Context

Resource query classes (e.g., `CustomFormatResourceQuery`, `QualitySizeResourceQuery`) need to
return service-specific data (Radarr vs Sonarr). The current design uses explicit methods
(`GetRadarr()`, `GetSonarr()`) with switch expressions in consumers. This pattern technically
violates the Open/Closed Principle since adding a new service (e.g., Lidarr) requires modifying
existing query classes and all consumer switch statements.

## Decision

Retain the explicit service-specific method pattern rather than refactoring to a keyed DI strategy
pattern with `IIndex<SupportedServices, T>`.

Current pattern:

```csharp
public class QualitySizeResourceQuery
{
    public IReadOnlyList<RadarrQualitySizeResource> GetRadarr() => ...;
    public IReadOnlyList<SonarrQualitySizeResource> GetSonarr() => ...;
}

// Consumer
var qualitySizes = config.ServiceType switch
{
    SupportedServices.Radarr => guide.GetRadarr(),
    SupportedServices.Sonarr => guide.GetSonarr(),
    _ => throw new InvalidOperationException(...),
};
```

## Rationale

- Adding new services is rare; Recyclarr has supported only Radarr and Sonarr for years
- Domain work dominates: Adding Lidarr requires API integration, new resource types, pipeline
  phases, and extensive testing - mechanical code changes are a small fraction
- Bounded modification set: ~6 query classes and ~15 consumer locations, all following the same
  predictable pattern
- Discoverability: `GetRadarr()` is self-documenting; `queries[serviceType].Get()` requires
  understanding Autofac's `IIndex<>` pattern
- Compile-time safety: Switch exhaustiveness checking catches missing cases; keyed DI fails at
  runtime if a key is unregistered
- Simplicity: Direct method calls are easier to trace and debug than DI indirection

## Alternatives Considered

### Keyed DI Strategies with IIndex

Register service-specific implementations keyed by `SupportedServices`:

```csharp
public interface IQualitySizeResourceQuery
{
    IReadOnlyCollection<QualitySizeResource> Get();
}

// Registration
builder.RegisterType<QualitySizeResourceQuery<RadarrQualitySizeResource>>()
    .Keyed<IQualitySizeResourceQuery>(SupportedServices.Radarr);

// Consumer
var qualitySizes = queries[config.ServiceType].Get();
```

Rejected because:

- Adds interface proliferation and DI complexity
- Benefits only manifest when adding services (rare event)
- For heterogeneous types (MediaNaming), still requires type-specific strategy handlers
- The abstraction cost exceeds the extensibility benefit

### Generic Bridge Pattern

Centralize service-to-type mapping in a resolver:

```csharp
public interface IResourceQueryBridge<TResource>
{
    IReadOnlyCollection<TResource> Get(SupportedServices service);
}
```

Rejected because:

- Moves the switch statement rather than eliminating it
- The resolver still needs modification for new services
- No OCP improvement, just indirection

## Consequences

- Adding a new service requires modifying query classes (add `GetNewService()` method) and consumer
  switch expressions
- This is an acceptable tradeoff: the modification pattern is predictable, searchable, and
  compiler-assisted
- The architecture document describes this as current reality without justification; this ADR
  preserves the reasoning for future maintainers
