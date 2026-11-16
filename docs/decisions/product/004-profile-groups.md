# PDR-004: Profile Groups

- **Upstream:** [PR #2561](https://github.com/TRaSH-Guides/Guides/pull/2561)

## Context

Guide quality profiles span categories (Standard, Anime, French, German, SQP), with no
machine-readable grouping for sync tools.

## Decision

The guides publish profile groups in `docs/json/{radarr,sonarr}/quality-profile-groups/groups.json`.
Recyclarr loads them (`QualityProfileGroupResourceQuery`) but nothing consumes them yet; they are
for organizing profile selection, not for sync behavior.

## Consequences

- Config: None
- Migration: not required

## History

- 2025-12-13: Upstream adds quality profile groups.
