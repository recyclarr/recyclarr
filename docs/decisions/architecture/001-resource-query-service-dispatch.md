# ADR-001: Resource queries dispatch on service type with a switch

- **Scope:** `src/Recyclarr.Core/ResourceProviders/Domain/*ResourceQuery.cs` and their consumers

## Context

Resource query classes (for example `CustomFormatResourceQuery`, `QualitySizeResourceQuery`) return
guide data for Radarr or Sonarr. A switch on the service type violates the Open/Closed Principle:
a new service means editing every query class. Recyclarr has supported only Radarr and Sonarr for
years, and any new service would need far more domain work (API client, resource types, pipeline
components) than query edits.

## Decision

Each resource query MUST select the service with a `switch` on `SupportedServices` inside the query
(`Get(SupportedServices serviceType)` dispatching to a private generic method). A query whose
services return unrelated types (`MediaNamingResourceQuery`) exposes one method per service
(`GetRadarr()`, `GetSonarr()`) instead. MUST NOT introduce keyed DI strategies or a generic
resolver to remove these switches; the compiler's exhaustiveness check is the extension mechanism.

## Rejected Options

- Keyed DI strategies (`IIndex<SupportedServices, T>`): adds one type per service per query and
  hides the call target, for a change that happens almost never.
- Generic bridge with a central resolver: same cost, plus indirection at every call site.

## Consequences

- A new service requires editing every query class and every per-service consumer; the edits are
  mechanical and found by searching for `SupportedServices`.
- Call sites stay direct and traceable.

## History

- 2025-11-27: Keep explicit per-service dispatch over keyed DI or a generic resolver.
- 2026-10-16: Record matched to code: queries switch internally; only naming keeps per-service
  methods.
