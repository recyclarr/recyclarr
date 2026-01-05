# PDR-003: CF Conflicts Metadata

- **Status:** Accepted
- **Date:** 2025-12-30
- **Upstream:** [Discord discussion](../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-3-cf-conflicts-metadata)

## Context

Some custom formats are mutually exclusive (e.g., SDR vs SDR no WEBDL, x265 HD vs x265 no HDR) but
there's no machine-readable way to express this. Users can inadvertently select conflicting CFs.

## Decision

Add `conflicts.json` files to TRaSH Guides at `docs/json/{radarr,sonarr}/conflicts.json` listing
mutually exclusive custom format pairs. Sync tools can use this to warn users about conflicting
selections.

**Implementation details pending**: The concept of a conflicts file is accepted, but the exact
schema (including updates to `metadata.json` and `metadata.schema.json`) is still being finalized
upstream.

## Affected Areas

- Config: None
- Commands: None
- Migration: Not required

## Consequences

- Users get warnings when selecting conflicting custom formats
- Requires new resource provider support in Recyclarr when upstream finalizes schema
- Can be implemented independently of other TRaSH Guides schema changes
