# ADR-010: Hand-written Sonarr and Radarr API contracts

- **Status:** Accepted
- **Date:** 2026-10-27
- **Supersedes:** [ADR-006](006-refit-refitter-api-clients.md)

## Context and Problem Statement

ADR-006 generated Refit interfaces and DTOs with Refitter from vendored Sonarr and Radarr OpenAPI
specs. A spec describes one server release, but users run any release, often newer than the vendored
copy. The generated clients failed in ways that came from typing fields Recyclarr does not use:

- New fields were dropped and reset on update, because the specs mark schemas
  `additionalProperties: false` (`10cf03cd`)
- A new enum value failed every sync. Radarr 7 reports `"authentication": "oidc"` in system status,
  and the generated `AuthenticationType` enum rejected it (PR #1010)
- `object` fields deserialized as `JsonElement` and broke custom format idempotency (`3f68da9a`)
- The specs needed rewriting before vendoring (`e1fca3f7`), plus a workflow to keep them current

The ADR-006 goal of surfacing upstream changes as compile errors did not hold either: errors
appeared only when the vendored spec was refreshed, not when users upgraded their server.

## Decision Drivers

- Sync must work against Sonarr and Radarr releases newer than Recyclarr
- Read-modify-write updates must not reset settings Recyclarr does not manage
- Keep Refit's typed interfaces, `DelegatingHandler` pipeline, and NSubstitute-friendly seams
- Recyclarr uses a small part of each API (about 15 endpoints per service)

## Considered Options

1. Keep generation and post-process specs further (remove `enum`, more rewrites)
2. Keep generation and add a tolerant enum converter
3. Hand-write lean Refit interfaces and DTOs for the endpoints and fields Recyclarr uses

## Decision Outcome

Chosen option: "Hand-write lean Refit interfaces and DTOs", because a contract that models only what
Recyclarr reads and writes cannot fail on what it does not model.

- `Recyclarr.Api.Radarr`, `Recyclarr.Api.Sonarr.V3`, and `Recyclarr.Api.Sonarr.V5` hold hand-written
  Refit interfaces with only the methods production calls. Test-only endpoints live in test
  projects.
- Every DTO derives from a per-namespace `ExtensibleResource` whose `[JsonExtensionData]` dictionary
  keeps unmodeled fields at every nesting level, so stashed DTOs (ADR-005) round-trip them.
- DTOs model no enums. A service value Recyclarr maps to its own enum (propers and repacks mode) is
  a string; unknown values map to "not set" and are sent back unchanged.
- No vendored specs or generation tooling. When a feature needs a new field or endpoint, read the
  upstream source or spec and add it by hand.
- Option 2 was rejected because an unknown-to-default converter loses the original value, and a PUT
  of the stashed DTO would then reset it.

### Consequences

- Good, because new server fields and values no longer fail a sync or get reset
- Good, because no spec-sync workflow, spec rewriting, or generator workarounds remain
- Good, because DTOs stay small enough to review
- Bad, because upstream renames or removals of modeled fields are not detected at build time; E2E
  tests against current Sonarr and Radarr images are the check
- Bad, because adding an endpoint or field means writing it by hand
