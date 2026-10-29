# PDR-006: CF Group Auto-Sync Contract

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

Recyclarr's auto-sync follows only the group-level `default` flag:

| Group `default` | Recyclarr behavior                          | User override               |
|-----------------|---------------------------------------------|-----------------------------|
| `true`          | Auto-sync to matching guide-backed profiles | `custom_format_groups.skip` |
| `false`/omitted | Do not auto-sync                            | `custom_format_groups.add`  |

- The `default` flag is the single source of truth; guide prose is informational only. A mismatch
  between prose and JSON is an upstream data bug, not a reason to special-case a group.
- Mandatory and Recommended both auto-sync; the distinction only helps users judge the impact of
  skipping a group.
- A group listed under `add` is not also auto-synced.

## Consequences

- Config: `custom_format_groups.skip[]` and `custom_format_groups.add[]`
- Migration: not required
- Auto-sync is deterministic and depends only on JSON data.

## History

- 2026-02-01: Auto-sync follows the group `default` flag; `skip` and `add` override it.
