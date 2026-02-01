# PDR-006: CF Group Auto-Sync Contract

- **Status:** Accepted
- **Date:** 2026-02-01
- **Upstream:** [Discord DM][upstream] with TRaSH Guides contributor

[upstream]: https://discordapp.com/channels/@me/1046139300401660005/1467565004663427102

## Context

TRaSH Guides uses a three-tier classification for CF groups that determines whether they should be
automatically synced to a quality profile:

| Tier        | Definition                                                    |
|-------------|---------------------------------------------------------------|
| Mandatory   | Profile won't work correctly without this group               |
| Recommended | Profile works fine without, but works better with it          |
| Optional    | Added only for specific circumstances (e.g., A-tier indexers) |

This classification exists conceptually in guide documentation but needs explicit representation in
the CF group JSON data for automation tools to implement correctly.

Discussion with a TRaSH Guides contributor clarified the intended mapping between classification and
JSON data:

- **Mandatory and Recommended**: Group has `default: true`; Recyclarr auto-syncs these
- **Optional**: Group has `default: false` or omits the field; Recyclarr does not auto-sync

A key finding during this discussion: guide prose documentation may label a CF group as "optional"
while the JSON has `default: true`, creating a semantic mismatch. Example: Audio Formats is
documented as optional for UHD Bluray + WEB profiles, but has `default: true` in the JSON. The
contributor acknowledged this as a data bug to be reviewed; it is not a Recyclarr design issue.

## Decision

Recyclarr's auto-sync behavior follows the group-level `default` flag:

| Group `default` | Recyclarr Behavior             | User Override                          |
|-----------------|--------------------------------|----------------------------------------|
| `true`          | Auto-sync to matching profiles | `custom_format_groups.skip` to opt-out |
| `false`/omitted | Do not auto-sync               | `custom_format_groups.add` to opt-in   |

This design correctly models the intended three-tier semantics:

- Users who want guide recommendations get Mandatory and Recommended groups automatically
- Deviations from recommendations require explicit configuration in either direction
- The `default` flag is the single source of truth; prose documentation is informational only

Recyclarr does not distinguish between Mandatory and Recommended at runtime; both auto-sync. The
distinction exists for guide documentation purposes to help users understand the impact of skipping
a group.

## Affected Areas

- Config: `custom_format_groups.skip[]` and `custom_format_groups.add[]` for user overrides
- Commands: None
- Migration: Not required

## Consequences

- Auto-sync behavior is deterministic and based solely on JSON data, not prose interpretation
- Users trust that `default: true` groups represent guide maintainer recommendations
- Semantic mismatches between prose and JSON are upstream data bugs to be fixed in TRaSH Guides
- Recyclarr's `skip`/`add` design remains stable; no changes needed to support the three-tier model
