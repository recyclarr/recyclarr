# PDR-001: CF Groups Exclude to Include Migration

- **Status:** Accepted
- **Date:** 2025-12-30
- **Upstream:** [Discord discussion](../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-1-exclude-to-include-migration)

## Context

TRaSH Guides CF Groups use `exclude` lists - groups apply to ALL quality profiles EXCEPT those
explicitly excluded. This fail-open design was chosen in 2022 assuming more profiles would be
included than excluded.

TRaSH reported making mistakes: "I made a mistake by not adding an exclude for a certain profile,
which resulted that the users got the wrong group added." The inverse thinking required for exclude
logic proved error-prone for maintainers.

## Decision

Switch CF Groups from `exclude` to `include` semantics. Groups will only apply to explicitly listed
quality profiles (fail-closed).

Recyclarr will update CF group parsing when the upstream migration is complete.

## Affected Areas

- Config: `custom_format_groups` implicit profile assignment becomes explicit
- Commands: None
- Migration: Not required - coordinated upstream deployment ensures compatibility

## Consequences

- CF group profile assignment becomes explicit rather than fail-open
- Easier for TRaSH Guides maintainers to reason about which profiles receive groups
- Recyclarr's `CfGroupResource` parsing will need updating when upstream migrates
