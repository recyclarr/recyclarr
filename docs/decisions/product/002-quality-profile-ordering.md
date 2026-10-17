# PDR-002: Quality Profile Ordering Inversion

- **Upstream:** [Discord discussion][upstream]

[upstream]: ../../reference/trash-guides-cf-groups-discord-2025-11-06.md#decision-2-quality-profile-ordering

## Context

The Sonarr and Radarr APIs order quality profile items bottom-to-top. Guide maintainers expect
top-to-bottom, the order the UI shows.

## Decision

Guide quality profile JSON lists qualities top-to-bottom. Recyclarr works top-to-bottom internally
and its gateways reverse the item order (recursively) after each GET and before each POST or PUT
(`QualityProfileApiExtensions`). MUST NOT expose API order outside the gateways.

## Consequences

- Config: None
- Migration: not required
- Guide quality profile JSON is easier to maintain.

## History

- 2025-12-30: Guide quality profiles switch to top-to-bottom order.
