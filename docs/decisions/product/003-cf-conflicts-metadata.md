# PDR-003: CF Conflicts Metadata

- **Upstream:** [Discord discussion][upstream]

[upstream]: ../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-3-cf-conflicts-metadata

## Context

Some custom formats are mutually exclusive (for example SDR versus SDR no WEBDL, x265 HD versus
x265 no HDR), and users can select both by mistake. The guides now publish
`docs/json/{radarr,sonarr}/conflicts.json` (schema `schemas/conflicts.schema.json`, paths declared
in `metadata.json`) for sync tools to use.

## Decision

Recyclarr treats `conflicts.json` as the source for warning users about conflicting custom format
selections. Recyclarr does not read the file yet; support needs a resource provider entry in
`RepoMetadata` and a planning outcome.

## Consequences

- Config: None
- Migration: not required
- Support can ship independently of other guide schema changes.

## History

- 2025-12-30: Upstream adds conflicts metadata for sync tools to warn on.
- 2026-10-16: Record matched to upstream: the file and schema are published.
