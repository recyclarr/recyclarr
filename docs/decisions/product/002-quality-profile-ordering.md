# PDR-002: Quality Profile Ordering Inversion

- **Status:** Accepted
- **Date:** 2025-12-30
- **Upstream:** [Discord discussion](../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-2-quality-profile-ordering)

## Context

TRaSH Guides quality profile JSON files order quality items bottom-to-top, matching the Sonarr/
Radarr API response format. This is counterintuitive for human maintainers who expect top-to-bottom
ordering.

## Decision

Invert quality ordering in TRaSH Guides JSON files to top-to-bottom (human-readable). Sync tooling
will reverse the order before sending to APIs.

## Affected Areas

- Config: None
- Commands: None
- Migration: Not required - Recyclarr already uses top-to-bottom internally and reverses before API

## Consequences

- TRaSH Guides quality profile JSON becomes easier to maintain
- No change needed in Recyclarr - already handles ordering internally
