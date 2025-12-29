# Quality Profile Sync

Quality Profile synchronization follows the standard pipeline pattern. This document covers
QP-specific behaviors in transaction and persistence phases. For plan phase details, see
[sync-plan-phase.md](sync-plan-phase.md). For the ID-first matching algorithm, see
[trash-id-cache-system.md](trash-id-cache-system.md).

## Profile Types

**Guide-backed profiles** (with `trash_id`) load from TRaSH Guides and use cache-based ID-first
matching.

**User-defined profiles** (name only) use name-based matching and are not cached.

## Three-Layer Precedence

For guide-backed profiles, properties merge with this precedence (lowest to highest):

1. **Service** - current profile state in Sonarr/Radarr
2. **Guide** - TRaSH Guide resource properties
3. **Config** - user YAML overrides

User-defined profiles skip the guide layer.

## Quality Item Organization

Quality ordering supports:

- `quality_sort: top` / `bottom` - user qualities position
- Quality groups - nested items under a group name
- Enabled/disabled state per quality

Qualities are inherited from guide resources when not specified in config.

## Language Passthrough (Radarr)

For Radarr profiles, language settings from guide resources pass through to the service. Sonarr does
not have language in quality profiles; such data is preserved via extra JSON passthrough.

## No Delete Feature

Unlike Custom Formats, Quality Profiles do not support automatic deletion. Deleting a profile has
major consequences for media organization (items assigned become invalid). Users must manually
delete unwanted profiles.

## Transaction Categories

- **Changed** - profiles requiring API calls (new or updated)
- **Unchanged** - profiles matching service state
- **NonExistent** - implicit profiles that don't exist in service
- **Invalid** - profiles failing validation
- **Conflicting** - guide-backed profiles with name collision (suggest `--adopt`)
- **Ambiguous** - multiple name matches

## Cache Rebuild States

QP cache rebuild produces these states:

**Changes**: Added, Adopted, Corrected, Removed

**Informational**: NotInService, Unchanged, Preserved, Ambiguous

## Future Work

**REC-26**: Full language configuration in YAML (currently passthrough only).
