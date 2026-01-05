---
status: accepted
date: 2025-01-04
upstream: docs/reference/trash-guides-cf-groups-discord-2025-11-06.md
---

# PDR-001: TRaSH Guides Schema Migration 2025

## Context

TRaSH Guides maintainers reached consensus on three schema changes (Discord, 2025-12-30):

1. **CF Groups exclude-to-include**: Switch from `exclude` lists (fail-open) to `include` lists
   (fail-closed). TRaSH noted difficulty maintaining exclude logic; yammes advocated for explicit
   profile assignment.

2. **Quality Profile ordering**: Invert item ordering from bottom-to-top (API format) to
   top-to-bottom (human-readable). voidpointer confirmed Recyclarr already reverses ordering
   internally.

3. **CF Conflicts metadata**: Add `conflicts.json` per service listing mutually exclusive custom
   formats (e.g., SDR vs SDR no WEBDL, x265 HD vs x265 no HDR). Enables validation warnings.

Additionally, Profile Groups (PR #2561) was merged on 2025-12-13, adding logical categories
(Standard, Anime, French, German, SQP) for quality profiles.

## Decision

Adopt all upstream schema changes. Recyclarr will:

1. Update CF group processing to use `include` semantics when upstream migrates (coordinated
   deployment with Notifiarr)
2. Continue current quality profile ordering (already top-to-bottom internally; no change needed)
3. Add validation warnings for conflicting CFs when `conflicts.json` becomes available upstream
4. Support profile groups for organizing quality profiles in future enhancements

## Affected Areas

- Config: `custom_format_groups` implicit profile assignment becomes explicit
- Commands: None
- Migration: Not required - changes are backward-compatible via upstream coordination

## Consequences

- CF group profile assignment becomes explicit rather than fail-open
- Quality profile JSON easier for upstream contributors to maintain
- Users get warnings when selecting conflicting custom formats
- Profile groups enable future UX improvements for profile organization
