# ADR-005: Service Gateway Layer for Sonarr/Radarr API Abstraction

- **Scope:** `src/Recyclarr.Core/Servarr/` (ports), `src/Recyclarr.Core/ServarrApi/` (gateways)

## Context

Sonarr and Radarr share a heritage but their APIs diverge. A single "Servarr" abstraction forced
dispatch hacks into shared pipeline code for every service-specific feature, and hand-written DTOs
with `[JsonExtensionData]` hid upstream schema changes: renames and removals produced no compile
error and silently lost data on PUT.

## Decision

Pipelines talk to services only through ports; each port has one gateway per service API target.

- **Port**: a domain interface that speaks only domain types (for example
  `IQualityProfileService`). Pipeline code injects ports, never API clients.
- **Gateway**: implements a port for one service API target (for example
  `SonarrV5QualityProfileGateway`). It owns the generated API client and maps between API DTOs and
  domain types in private methods. MUST NOT extract the mapping into a separate translator class;
  it has one caller and is tested through the gateway.
- **Hydrated resource**: for read-modify-write resources, the gateway stashes each fetched DTO by
  resource ID, applies domain changes to the stashed DTO on persist, and PUTs the complete object.
  Domain types carry no `[JsonExtensionData]`.
- Gateways are registered per lifetime scope (`RegisterServiceGateway`), so stashed state lives for
  one instance sync and is never shared.

ADR-023 owns which fields live on shared versus service-specific types. ADR-028 owns how the API
target is selected.

## Rejected Options

- Extend the unified Servarr abstraction: exceptions keep accreting in shared code.
- Separate Sonarr and Radarr codepaths with no sharing: duplicates the pipeline flow that is common.
- Re-fetch the resource during persist instead of stashing: adds network calls and failure modes
  for staleness that cannot occur within one sync.

## Consequences

- Pipeline code has no service awareness.
- Upstream API changes surface as compile errors in gateway mappers.
- Gateways for near-identical schemas (custom formats) are thin mapping code.
- The stash is implicit state that readers of gateway code must know about.

## History

- 2026-02-26: Introduce ports and per-service gateways with hydrated resources.
- 2026-02-27: Keep mapping private to the gateway; no translator class.
- 2026-10-03: Pipeline shape rules move to ADR-023.
