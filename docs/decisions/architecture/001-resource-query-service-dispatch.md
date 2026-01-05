# ADR-001: Resource Query Service Dispatch Pattern

- **Status:** Accepted
- **Date:** 2025-11-27

## Context and Problem Statement

Resource query classes (e.g., `CustomFormatResourceQuery`, `QualitySizeResourceQuery`) need to
return service-specific data (Radarr vs Sonarr). The current design uses explicit methods
(`GetRadarr()`, `GetSonarr()`) with switch expressions in consumers. This technically violates the
Open/Closed Principle since adding a new service requires modifying existing query classes and all
consumer switch statements.

## Decision Drivers

- Adding new services is rare; Recyclarr has supported only Radarr and Sonarr for years
- Domain work dominates any new service: API integration, resource types, pipeline phases, testing
- Compile-time safety via switch exhaustiveness checking
- Discoverability: `GetRadarr()` is self-documenting vs `queries[serviceType].Get()`

## Considered Options

1. Retain explicit service-specific methods (`GetRadarr()`, `GetSonarr()`)
2. Keyed DI strategies with `IIndex<SupportedServices, T>`
3. Generic bridge pattern with centralized resolver

## Decision Outcome

Chosen option: "Retain explicit service-specific methods", because the abstraction cost exceeds the
extensibility benefit given how rarely new services are added.

### Consequences

- Good, because direct method calls are self-documenting and easy to trace
- Good, because compiler catches missing cases via switch exhaustiveness
- Good, because no interface proliferation or DI complexity
- Bad, because adding a new service requires modifying ~6 query classes and ~15 consumer locations
- Bad, because the modification pattern is manual (though predictable and searchable)
