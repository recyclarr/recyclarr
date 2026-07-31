# ADR-013: Refitter for the Recyclarr API client

- **Status:** accepted
- **Date:** 2026-07-24

## Context and Problem Statement

Recyclarr now publishes its own HTTP API (ADR-008, ADR-011) and commits the generated OpenAPI spec to
source control. The CLI will consume that API as a typed client (REC-142). The initial
implementation generated this client with Kiota, which produces a fluent request-builder tree
mirroring the URL structure. That choice sits alongside [ADR-006](006-refit-refitter-api-clients.md),
which established Refit and Refitter as the generation stack for the Sonarr and Radarr clients and
explicitly considered and rejected Kiota. The result is two OpenAPI generators in one solution for
three OpenAPI clients.

## Decision Drivers

- ADR-006 already settled OpenAPI client generation for this codebase; a second generator needs to
  justify itself against that precedent
- Refit's runtime (`Refit.HttpClientFactory`) already ships in `Recyclarr.Core` and `Recyclarr.Cli`;
  Kiota adds `Microsoft.Kiota.Bundle` as a second HTTP client stack carried for one client
- The spec is OpenAPI 3.1.1, which rules out generators still limited to 3.0
- ADR-011 path-versions the API (`/api/v1/...`), and external consumers on independent release
  cadences need v1 and v2 to stay distinguishable
- Nothing consumes the generated client yet beyond a factory and a smoke test, so switching cost is
  at its minimum right now and rises once REC-142 begins

## Considered Options

1. Keep Kiota for the self-client, accepting two generators in the solution
2. Generate the self-client with Refitter, matching ADR-006
3. Hand-write a facade over the Kiota client to flatten call sites

## Decision Outcome

Chosen option: "generate the self-client with Refitter", because it reuses a stack the product
already ships and collapses the solution back to one OpenAPI generation pattern.

Refitter v2.0.0 replaced its OpenAPI reader with `Microsoft.OpenApi` 3.x, which is where 3.1 support
lives. The solution is already pinned to that version for the Sonarr and Radarr clients, so no
upgrade is required.

### Versioning

Each API version gets its own `.refitter` configuration, scoped by path regex and emitted into a
version-specific namespace:

```json
{
  "openApiPath": "../Recyclarr.Server/Recyclarr.Server.json",
  "namespace": "Recyclarr.Client.V1",
  "includePathMatches": ["^/api/v1/.*"],
  "multipleInterfaces": "ByTag"
}
```

A v2 client is a second configuration with `^/api/v2/.*` and `Recyclarr.Client.V2`. Consumers pick a
version once, at import or DI registration, and the version segment stays in the Refit route
attribute rather than appearing at every call site. This keeps versions distinguishable for external
consumers without threading the version through every call.

### Consequences

- Good, because the self-client adds no runtime dependency beyond what Recyclarr already ships
- Good, because one generator, one configuration format, and one call-site idiom cover all three
  OpenAPI clients
- Good, because interface-per-tag output is directly mockable with NSubstitute, matching how the
  Sonarr and Radarr clients are tested
- Good, because version selection happens once per consumer instead of at every call
- Bad, because it discards the Kiota integration built in REC-151, including its MSBuild hash-stamp
  generation target
- Bad, because Refitter remains a smaller project than Kiota with a single primary maintainer; the
  fallback named in ADR-006 (hand-writing Refit interfaces from the spec) applies here too
- Bad, because a second `.refitter` configuration is needed for each future API version, whereas
  Kiota would have picked up new version paths from the same spec automatically
