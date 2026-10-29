# PDR-001: CF Groups Exclude to Include Migration

- **Upstream:** [Discord discussion][upstream]

[upstream]: ../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-1-exclude-to-include-migration

## Context

TRaSH Guides CF groups used `exclude` lists: a group applied to every quality profile except those
listed. Maintainers found this fail-open design error-prone; a missing exclude gave users the wrong
group.

## Decision

CF groups apply only to the quality profiles listed in `quality_profiles.include` (fail-closed).
Recyclarr parses only `include` (`CfGroupResource`) and has no guide-side `exclude` handling.

## Consequences

- Config: None; the user-side `custom_format_groups` keys are unaffected (PDR-005, PDR-006).
- Migration: not required; the upstream change was deployed in coordination with Recyclarr.
- Maintainers state which profiles receive each group.

## History

- 2025-12-30: Upstream switches CF groups from `exclude` to `include`.
- 2026-10-16: Record matched to code: upstream migration done; Recyclarr parses only `include`.
