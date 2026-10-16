# ADR-028: API version selection by server major version

- **Status:** Accepted
- **Date:** 2026-10-13

## Context and Problem Statement

Sonarr v5 ships a new API generation (`/api/v5`) and keeps serving the deprecated v3 API. The v3 API
on Sonarr v5 no longer matches v5 semantics: quality sizes moved from global quality definitions
onto quality profile items, and v3 writes silently dropped data that v5 stores. Sonarr v4 only
serves v3. Recyclarr must drive both majors correctly from one build.

## Decision Drivers

- Pipelines and domain ports must stay version-agnostic ([ADR-005](005-service-gateway-layer.md))
- Generated clients must track each upstream spec without manual DTO edits
  ([ADR-006](006-refit-refitter-api-clients.md))
- A version mismatch must fail loudly, never fall back to the wrong API

## Considered Options

1. Drive every Sonarr version through v3 and patch v5 differences in gateways
2. Select gateways per instance by server major version, one generated client per API generation
3. A separate `Recyclarr.Api.SonarrV5` project

## Decision Outcome

Chosen option: "Select gateways per instance by server major version", because each API generation
then has an exact, generated contract and version differences stay inside gateways.

- `Recyclarr.Api.Sonarr` holds two Refitter configs, `V3/` and `V5/`, generating namespaces
  `Recyclarr.Api.Sonarr.V3` and `Recyclarr.Api.Sonarr.V5`.
- The capability check maps the detected version to a `ServiceApiTarget` (`SonarrV3`, `SonarrV5`,
  `RadarrV3`). Gateways are keyed by target; the instance processor resolves ports from a child
  scope that registers the detected service. Resolving a port before detection fails.
- Service-owned quality profile fields (ADR-023) are keyed by target as well, so Sonarr v5
  per-profile quality sizes have their own component.
- A sync operation that has no gateway on a target resolves that gateway lazily and is skipped by
  its plan there (the quality definition operation on Sonarr v5, whose planner assigns sizes to
  profiles instead).
- The version probe stays on `/api/v3/system/status`, which both majors serve.

### Consequences

- Good, because each Sonarr major gets a lossless, spec-accurate client
- Good, because pipelines and domain ports are unchanged
- Bad, because Sonarr gateways and mappers exist once per API generation with near-identical code
- Bad, because the probe depends on Sonarr keeping `/api/v3/system/status`. Ceiling: Sonarr removing
  the v3 API. Trigger to replace: switch the probe to the unversioned `GET /api` (`current` field)
  when v3 removal is announced.
