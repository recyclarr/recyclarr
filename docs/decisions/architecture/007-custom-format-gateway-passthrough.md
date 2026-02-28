# ADR-007: Custom Format Gateway Passthrough (Exception to DTO/Domain Split)

- **Status:** accepted
- **Date:** 2026-02-28

## Context and Problem Statement

ADR-005 establishes the Port + Gateway pattern where gateways map between service DTOs and domain
types. Custom Formats are the first pipeline where this split creates friction rather than value.
The guide JSON format matches the Sonarr/Radarr API format (identical schemas), CF sync is strictly
one-way (guide overwrites service state), and the type graph is complex (3 nested types with custom
JSON converters). Forcing a DTO/domain split here would duplicate `CustomFormatResource`,
`CustomFormatSpecificationData`, and `CustomFormatFieldData` with no functional benefit.

## Decision Drivers

- `CustomFormatResource` already serves as both the guide resource type and the API payload type;
  the guide JSON is authored in the exact format the Sonarr/Radarr API accepts
- CF sync is one-way push: the guide is the complete source of truth for what a custom format should
  look like; there is no fetch-modify-push requiring round-trip safety or stashed DTOs
- The type graph includes custom JSON converters (`FieldsArrayJsonConverter`,
  `NondeterministicValueConverter`) that handle the one known divergence between guide and API
  formats: `fields` is an object in guide JSON but an array in API JSON
- The CF -> QP pipeline dependency relies on `PlannedCustomFormat.Resource.Id` being mutated
  in-place after CF creation; a domain type split would require reworking this shared reference
  chain
- Duplicating 3 types with custom converters and adding identity-mapping methods violates YAGNI

## Considered Options

1. Passthrough: gateway uses `CustomFormatResource` directly as the port's domain type (no
   DTO/domain split, no stash)
2. Full DTO/domain split: create `ServiceCustomFormat`, `ServiceCustomFormatSpecification`,
   `ServiceCustomFormatField` DTOs with gateway mapping (consistent with other phases)

## Decision Outcome

Chosen option: "Passthrough", because the conditions that justify the DTO/domain split in other
pipelines do not apply to Custom Formats.

### General criteria for when passthrough is appropriate

A pipeline may use the existing resource type as its port's domain type (skipping the DTO/domain
split) when ALL of the following hold:

1. **One-way sync model.** The guide/config defines the complete desired state; the gateway sends it
   as-is. There is no fetch-modify-push cycle requiring stashed DTOs or field preservation.
2. **Identical or trivially-bridged schemas.** The guide type and API type share the same shape,
   with any format differences handled at the serialization level (converters, attributes) rather
   than in gateway mapping.
3. **Complex nested type graph.** The type has non-trivial nested structures (custom converters,
   polymorphic fields) where duplication would be expensive and error-prone.

When any of these conditions is absent, the standard DTO/domain split from ADR-005 applies. For
example, Quality Profiles have fetch-modify-push semantics (Radarr's `language` field must be
preserved on round-trip) and service-specific divergence, so they require the full split.

### Guide vs API JSON divergence

The `fields` property in `CustomFormatSpecificationData` differs between guide and API format:

- Guide JSON: `fields` is an object with key-value pairs (`{"value": "\\bAV1\\b"}`)
- API JSON: `fields` is an array of objects (`[{"name": "value", "value": "...", ...}]`)

`FieldsArrayJsonConverter` handles this transparently: it accepts both formats on read and always
serializes as the array format. This keeps the divergence at the serialization level, not the
gateway mapping level.

### Impact on related issues

[Issue #219][gh-219] (float deserialization in `SizeSpecification`) is a bug in
`NondeterministicValueConverter`, not an architectural issue. The fix (handling
`JsonValueKind.Number` with decimals) is the same regardless of whether the gateway uses passthrough
or a DTO/domain split. A split would only move the converter to a service DTO type and add a mapping
step that also has to handle `object?` value conversion.

### Consequences

- Good, because no duplication of 3 complex nested types with custom JSON converters
- Good, because the CF -> QP mutation dependency (`PlannedCfScore.ServiceId` reads
  `PlannedCustomFormat.Resource.Id`) continues to work without reworking reference chains
- Good, because gateways remain thin wrappers that provide the DI seam for keyed resolution and
  future Refit migration without adding mapping boilerplate
- Bad, because CF gateways deviate from the DTO/domain split pattern established in other phases
- Bad, because if CF ever needs fetch-modify-push semantics (preserving API fields not in the
  guide), the gateway will need to be retrofitted with stashed DTOs and mapping; the gateway
  boundary is already in place as the natural location for this change

[gh-219]: https://github.com/recyclarr/recyclarr/issues/219
