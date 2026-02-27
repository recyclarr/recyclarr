# ADR-005: Service Adapter Layer for Sonarr/Radarr API Abstraction

- **Status:** Accepted
- **Date:** 2026-02-26

## Context and Problem Statement

Recyclarr treats Sonarr and Radarr as a single "Servarr" API through a unified abstraction
(`IServarrRequestBuilder`). This assumption is increasingly inaccurate. Media naming required three
dispatch mechanisms (enum switch, keyed DI, pattern matching) to handle divergent schemas. Quality
profiles will need similar workarounds when Radarr language support is added. Each new
service-specific feature accretes exception handling onto a unified abstraction until the exceptions
outnumber the rule.

A secondary concern is the read-modify-write round-trip: current DTOs use `[JsonExtensionData]` to
capture unknown API fields for lossless PUT operations. This works but makes the DTOs change-blind
-- upstream schema changes (renames, removals) produce no compile errors and cause silent data loss.

## Decision Drivers

- Sonarr and Radarr APIs share a common heritage but are diverging over time
- Service-specific features (media naming, Radarr profile languages) should not require dispatch
  hacks in shared pipeline code
- The pipeline architecture (fetch, config, transaction, preview, persist) is sound and should be
  preserved
- Future migration to generated API clients (Refitter/Refit from OpenAPI specs) requires a clean
  boundary between service-specific types and domain types
- Round-trip safety: PUT operations must not silently drop fields the service expects

## Considered Options

1. Introduce a Port + Adapter + Translator layer between pipelines and HTTP clients
2. Continue extending the unified Servarr abstraction with per-feature dispatch
3. Replace the unified abstraction with fully separate Sonarr/Radarr codepaths (no sharing)

## Decision Outcome

Chosen option: "Port + Adapter + Translator layer", because it preserves pipeline sharing for
genuinely common features while giving service-specific features clean separation.

The architecture introduces three components per feature:

- **Port**: Domain interface defined in the pipeline/domain layer, speaks only domain types (e.g.
  `IQualityProfileService`). This is what pipeline phases inject.
- **Translator**: Static, stateless class containing pure mapping functions between service DTOs and
  domain types (e.g. `SonarrQualityProfileTranslator`).
- **Adapter**: Infrastructure class implementing the port for a specific service. Owns the HTTP
  client, calls the translator, and manages read-modify-write state (e.g.
  `SonarrQualityProfileAdapter`).

### Adapter state management

Adapters use the Hydrated Resource pattern: during fetch, the adapter stashes the original service
DTO in an internal dictionary keyed by resource ID. During persist, the adapter retrieves the
stashed DTO, applies domain changes via the translator, and PUTs the complete object back. This
eliminates the need for `[JsonExtensionData]` on domain types while preserving lossless round-trips.

Adapter instances are scoped to one sync operation (`InstancePerLifetimeScope`), matching the
existing API service lifetime. State is created during fetch, consumed during persist, and discarded
when the sync completes. No cross-sync state, no shared caches, no concurrency concerns.

The alternative (fetch-in-update: the adapter re-GETs the resource during persist instead of
stashing it) was rejected because it adds failure modes and network calls to solve a staleness
problem that does not exist within a single sync operation.

### Shared vs. service-specific pipeline boundary

The litmus test for whether a feature uses a shared pipeline with adapters or gets service-specific
pipelines: **Is there a meaningful shared domain concept, or just a shared endpoint path?**

- Same resource with service-specific properties: shared pipeline + adapter. The adapter maps
  service-specific fields into optional properties on the domain type. Example: quality profiles
  (Radarr adds language; the domain type has nullable `Language`; the Sonarr adapter leaves it
  null).
- Different resource concepts behind a shared path: service-specific pipelines. Example: media
  naming (Sonarr episode naming and Radarr movie naming are unrelated domain concepts that happen to
  live at `/api/v3/config/naming`).

### Consequences

- Good, because pipeline phases code against domain types with no service awareness
- Good, because service-specific features get their own pipelines without dispatch hacks
- Good, because adapters are the natural seam for future Refit/Refitter migration
- Good, because upstream API changes surface as compile errors in translators, not silent data loss
- Bad, because adapters for identical schemas (custom formats) are thin pass-through boilerplate
- Bad, because the stashed DTO dictionary is implicit state that must be understood when reading
  adapter code
