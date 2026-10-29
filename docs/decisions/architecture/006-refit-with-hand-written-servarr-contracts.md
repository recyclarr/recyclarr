# ADR-006: Refit clients with hand-written Sonarr and Radarr contracts

- **Scope:** `src/Recyclarr.Api.Sonarr`, `src/Recyclarr.Api.Radarr`, outbound HTTP in Core and
  Server

## Context

Recyclarr uses a small part of the Sonarr and Radarr APIs (about 15 endpoints per service), but
users run any server release, often newer than Recyclarr. Clients generated from a vendored OpenAPI
spec typed every field of one release: a new enum value failed every sync (Radarr 7 reports
`"authentication": "oidc"` in system status), new fields were dropped and reset on update because
the specs mark schemas `additionalProperties: false`, and `object` fields broke custom format
idempotency. Spec changes surfaced as compile errors only when the vendored copy was refreshed, not
when users upgraded their server.

## Decision

All outbound HTTP uses Refit interfaces on `HttpClient`.

- Sonarr and Radarr clients are hand-written Refit contracts (`Recyclarr.Api.Radarr`,
  `Recyclarr.Api.Sonarr.V3`, `Recyclarr.Api.Sonarr.V5`) that model only the endpoints and fields
  production reads or writes. Test-only endpoints live in test projects.
- Every DTO derives from a per-namespace `ExtensibleResource` whose `[JsonExtensionData]` dictionary
  keeps unmodeled fields at every nesting level, so stashed DTOs (ADR-005) round-trip them.
- DTOs MUST NOT model enums. A service value Recyclarr maps to its own enum (propers and repacks
  mode) is a string; unknown values map to "not set" and are sent back unchanged.
- MUST NOT vendor specs or add generation tooling for these services. When a feature needs a new
  field or endpoint, read the upstream source or spec and add it by hand.
- Apprise also uses a hand-written Refit interface (`IAppriseApi`).
- Cross-cutting HTTP concerns are `DelegatingHandler` subclasses (for example `HttpLoggingHandler`),
  not per-call code.

Generation fits only where Recyclarr owns both the server and its spec; ADR-013 covers that client.

## Rejected Options

- Generate clients from vendored specs with Refitter: types every field of one server release, so
  newer releases fail the sync or have settings reset.
- Keep generation and post-process specs further (strip `enum`, more rewrites): more spec rewriting
  and a sync workflow for the same fragility.
- Keep generation with a tolerant enum converter: mapping an unknown value to a default loses the
  original, and a PUT of the stashed DTO then resets it.
- RestEase: single maintainer, Newtonsoft by default.
- Stay on Flurl: no typed interfaces and harder test doubles.

## Consequences

- New server fields and values no longer fail a sync or get reset.
- Refit interfaces stay directly mockable with NSubstitute, and DTOs stay small enough to review.
- Upstream renames or removals of modeled fields are not detected at build time; E2E tests against
  current Sonarr and Radarr images are the check.
- Adding an endpoint or field means writing it by hand.

## History

- 2026-02-26: Replace Flurl with Refit; generate Sonarr and Radarr clients with Refitter.
- 2026-03-03: Flurl removed from the codebase.
- 2026-10-27: Hand-write Sonarr and Radarr contracts; drop vendored specs and their generation.
