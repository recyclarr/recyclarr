# ADR-028: API version selection by server major version

- **Scope:** `src/Recyclarr.Core/Compatibility/`, gateway registration, `src/Recyclarr.Api.Sonarr`

## Context

Sonarr v5 ships a new API generation (`/api/v5`) and still serves the deprecated v3 API, but v3 on
Sonarr v5 no longer matches v5 semantics: quality sizes moved onto quality profile items, and v3
writes silently dropped data that v5 stores. Sonarr v4 serves only v3. Pipelines and ports must stay
version-agnostic (ADR-005), and each API generation needs its own client contract (ADR-006).

## Decision

Select gateways per instance by the detected server major version, with one client contract per API
generation. A version mismatch MUST fail loudly; never fall back to another API generation.

- `Recyclarr.Api.Sonarr` holds two client contracts, namespaces `Recyclarr.Api.Sonarr.V3` and
  `Recyclarr.Api.Sonarr.V5`.
- The capability check maps the detected version to a `ServiceApiTarget` (`SonarrV3`, `SonarrV5`,
  `RadarrV3`). Gateways are keyed by target; the instance processor resolves ports from a child
  scope that registers the detected service. Resolving a port before detection fails.
- Service-owned quality profile fields (ADR-023) are keyed by target as well, so Sonarr v5
  per-profile quality sizes have their own component.
- A sync operation that has no gateway on a target resolves that gateway lazily and is skipped by
  its plan there (the quality definition operation on Sonarr v5, whose planner assigns sizes to
  profiles instead).
- The version probe stays on `/api/v3/system/status`, which both majors serve.

## Rejected Options

- Drive every Sonarr version through v3 and patch v5 differences in gateways: v3 on Sonarr v5 drops
  data.
- A separate `Recyclarr.Api.SonarrV5` project: a second project for one service's API generations.

## Consequences

- Each Sonarr major gets a client that matches its API generation; pipelines and ports are
  unchanged.
- Sonarr gateways and mappers exist once per API generation with near-identical code.
- The probe depends on Sonarr keeping `/api/v3/system/status`. Ceiling: Sonarr removing the v3 API.
  Trigger to replace: switch the probe to the unversioned `GET /api` (`current` field) when v3
  removal is announced.

## History

- 2026-10-13: Select Sonarr gateways by server major; generate V3 and V5 clients.
