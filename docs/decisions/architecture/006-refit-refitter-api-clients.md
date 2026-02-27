# ADR-006: Refit + Refitter for API Client Generation

- **Status:** Accepted
- **Date:** 2026-02-26

## Context and Problem Statement

Recyclarr uses Flurl as its sole HTTP client library for all external API communication (Sonarr,
Radarr, Apprise). Flurl is deeply integrated across 18 production files with custom event handlers,
per-client serializer configuration, and URL sanitization. While functional, the Flurl layer couples
Recyclarr to hand-maintained DTOs and imperative HTTP call patterns. Both Sonarr and Radarr publish
official OpenAPI 3.0 specifications (162 and 164 endpoints respectively), which are currently
unused.

## Decision Drivers

- Sonarr and Radarr maintain official OpenAPI specs generated via Swashbuckle; these are the
  authoritative API contracts
- Hand-maintained DTOs with `[JsonExtensionData]` are change-blind: upstream renames or removals
  produce no compile errors
- The adapter layer introduced in [ADR-005](005-service-adapter-layer.md) creates a natural seam for
  replacing the HTTP client without affecting pipeline code
- Type-safe interfaces improve discoverability and catch route/method mismatches at compile time
- Interface-based API clients are directly mockable with NSubstitute, simplifying test setup
  compared to `Flurl.Http.Testing`

## Considered Options

1. Refit with Refitter-generated interfaces and DTOs from OpenAPI specs
2. Refit with hand-written interfaces and existing DTOs
3. RestEase (Refit alternative, single maintainer, Newtonsoft default)
4. OpenAPI code generation via Kiota, NSwag, or openapi-generator
5. Remain on Flurl

## Decision Outcome

Chosen option: "Refit with Refitter", because spec-driven generation surfaces upstream API changes
as compile errors and eliminates hand-maintenance of service DTOs.

### Project structure

Each upstream service with an OpenAPI spec gets a dedicated generated library:

- `Recyclarr.Api.Sonarr` -- Refitter generates Refit interfaces and contracts from the Sonarr
  OpenAPI spec
- `Recyclarr.Api.Radarr` -- same, from the Radarr OpenAPI spec

Refitter configuration (`.refitter` files) uses `includePathMatches` to generate only the endpoints
Recyclarr consumes rather than the full 160+ endpoint surface. Generated types are the
service-specific DTOs that adapters (ADR-005) translate to/from domain types.

Apprise has no OpenAPI spec and a trivial API surface (single POST endpoint). Its Refit interface is
hand-written.

### Interface decomposition

Refitter's `multipleInterfaces: ByTag` or `includePathMatches` options decompose the generated
output into logical domain-scoped interfaces (e.g. custom formats, quality profiles, naming) rather
than one monolithic interface per service.

### Migration sequencing

The Refit migration is sequenced after the adapter layer (ADR-005) is in place. Adapters provide the
seam: Flurl calls inside adapters are replaced with Refit interface calls without touching pipeline
code. Cross-cutting concerns (request logging, URL sanitization, auth headers, SSL certificate
bypass) migrate from Flurl event handlers to `DelegatingHandler` subclasses in the `HttpClient`
pipeline.

### Consequences

- Good, because upstream API changes surface as compile errors in generated code
- Good, because Refit interfaces are directly mockable without HTTP interception libraries
- Good, because source-generated implementations have no runtime reflection
- Good, because `DelegatingHandler` is a standard .NET pattern for HTTP cross-cutting concerns
- Bad, because generated DTOs do not carry `[JsonExtensionData]`; round-trip safety relies on the
  adapter's hydrated resource pattern (ADR-005) instead
- Bad, because two generated projects add build-time generation steps and NSwag transitive
  dependencies
- Bad, because Refitter (375 stars, primarily one maintainer) is less established than Refit itself;
  fallback is hand-writing Refit interfaces from the specs
