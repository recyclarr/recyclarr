# ADR-026: Stay on Refitter rather than move OpenAPI clients to Kiota

- **Status:** accepted
- **Date:** 2026-10-10

## Context and Problem Statement

Refitter reads every spec through NSwag, whose schema library NJsonSchema types the
`x-enumDescriptions` extension as an array ([NJsonSchema#1943][njs-1943]). The object form that
docusaurus-theme-openapi-docs renders for the wiki API reference fails the whole document load, so
the self-client in `Recyclarr.Client` no longer builds. Contrary to ADR-013, Refitter 2.3.0 does not
read with Microsoft.OpenApi: its Microsoft.OpenApi path only resolves multi-file specs and then
hands the result back to NSwag. NJsonSchema has caused other Refitter load failures too (numeric
bounds, recursive schemas), so moving all three clients to Kiota, which reads with Microsoft.OpenApi
3.x, was evaluated with a working Kiota 1.36.0 build of the self-client.

## Decision Drivers

- The self-client must load the spec the wiki renders, including object-valued vendor extensions
- The CLI renders server responses directly; required fields must not surface as nullable
- Sonarr and Radarr clients are mocked with NSubstitute through their Refit interfaces (ADR-006)
- One generator for all OpenAPI clients (ADR-013)

## Considered Options

1. Move all generated clients to Kiota
2. Stay on Refitter and get the NJsonSchema failure fixed or worked around upstream

## Decision Outcome

Chosen option: "Stay on Refitter", because Kiota fixes one parser bug but adds several lasting
costs, while the Refitter problem is a single bug with a narrow fix.

Kiota loaded the spec without error, but the trial surfaced these costs:

- **All model properties are nullable**, including those listed in `required`. Maintainers chose
  this deliberately because OpenAPI cannot tell "never null" from "not selected" or "optional on
  PATCH" ([kiota#3846][kiota-3846]). The fix is tracked for Kiota v2 with no timeline
  ([kiota#3911][kiota-3911]). For the self-client that reasoning does not apply (the server enforces
  `required`), and every CLI renderer read would need a null guard or a mapping layer. Refitter with
  `RespectNullableAnnotations` fails at deserialization instead.
- **No interfaces.** Kiota emits concrete request-builder classes and its maintainers decline to
  generate interfaces ([kiota#5560][kiota-5560]). Tests must fake `IRequestAdapter` or the HTTP
  handler instead of substituting a Refit interface.
- **`anyOf` with a discriminator becomes an intersection wrapper** that fills in every variant, so
  the CLI could not tell which polymorphic response it received. Only `oneOf` dispatches on the
  discriminator.
- **Path segments appear at call sites** (`client.Api.V3.Qualityprofile`). Maintainers decline a
  prefix-stripping option ([kiota#4004][kiota-4004]); the workaround is rewriting the spec.
- **No MSBuild integration.** The supported package was retired ([kiota#3005][kiota-3005]), so
  generation needs a hand-written build target.
- **Status codes are not returned** on success, so callers cannot branch on 200 versus 202.

The NJsonSchema failure goes to Refitter's maintainer, who already sanitizes specs before NSwag
reads them (for example the numeric-bounds fix) and plans to remove the NSwag and NJsonSchema
dependencies ([refitter#1310][refitter-1310]).

### Consequences

- Good, because the CLI keeps non-nullable contracts and fails fast on a broken response
- Good, because Refit interfaces stay directly mockable and call sites stay free of path segments
- Good, because all three clients keep one generator and configuration format
- Bad, because the self-client stayed blocked on an upstream fix for object-valued
  `x-enumDescriptions` until Refitter 2.3.1-preview.120 ([refitter#1336][refitter-1336])
- Bad, because NJsonSchema remains in the read path until Refitter removes it, and its 3.1 support
  is incomplete

Revisit when Kiota generates non-nullable `required` properties for C#, or if Refitter's NSwag
removal stalls while NJsonSchema failures keep blocking builds.

[njs-1943]: https://github.com/RicoSuter/NJsonSchema/issues/1943
[kiota-3846]: https://github.com/microsoft/kiota/issues/3846
[kiota-3911]: https://github.com/microsoft/kiota/issues/3911
[kiota-5560]: https://github.com/microsoft/kiota/issues/5560
[kiota-4004]: https://github.com/microsoft/kiota/issues/4004
[kiota-3005]: https://github.com/microsoft/kiota/issues/3005
[refitter-1310]: https://github.com/christianhelle/refitter/issues/1310
[refitter-1336]: https://github.com/christianhelle/refitter/issues/1336
