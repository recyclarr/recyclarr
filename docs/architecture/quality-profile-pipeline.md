# Quality Profile Sync

Quality Profile synchronization is handled by `QualityProfileSyncOperation`. This document covers
QP-specific behaviors in compute and persist. For plan phase details, see
[sync-plan-phase.md](sync-plan-phase.md). For the ID-first matching algorithm, see
[trash-id-state-system.md](trash-id-state-system.md).

## Profile Types

**Guide-backed profiles** (with `trash_id`) load from TRaSH Guides and use state-based ID-first
matching. Multiple profiles can share the same `trash_id` with different names and config overrides.
See [quality-profile-state-resolution.md](quality-profile-state-resolution.md) for the two-pass
algorithm that supports this.

**User-defined profiles** (name only) use name-based matching and are not tracked in state.

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

## Service-Owned Fields

The pipeline has one sync operation for both services. Fields only one service has live on that
service's types ([ADR-023][adr-023]), and their behavior lives in `IQualityProfileServiceFields`,
which DI resolves per detected API target ([ADR-028][adr-028]):

- Radarr (`RadarrQualityProfileFields`): profile language. The guide's `language` applies when the
  instance offers a language with that name; otherwise the profile keeps its current language. A
  language difference alone makes the profile changed, so drift is restored.
- Sonarr v4 (`SonarrV3QualityProfileFields`, v3 API): no service-owned fields. Sonarr guide JSON
  that contains a `language` is ignored.
- Sonarr v5 (`SonarrV5QualityProfileFields`): per-profile quality sizes, stored on
  `SonarrV5QualityProfileData` keyed by quality name. The planner chooses each profile's size source
  ([PDR-008][pdr-008]); this component converts the planned sizes to service values and writes them
  to qualities the profile contains. Other qualities keep their sizes. A size difference alone makes
  the profile changed.

[adr-023]: ../decisions/architecture/023-service-owned-fields-in-shared-pipelines.md
[adr-028]: ../decisions/architecture/028-api-version-selection-by-server-major.md
[pdr-008]: ../decisions/product/008-sonarr-v5-per-profile-quality-sizes.md

## No Delete Feature

Unlike Custom Formats, Quality Profiles do not support automatic deletion. Deleting a profile has
major consequences for media organization (items assigned become invalid). Users must manually
delete unwanted profiles.

## Transaction Categories

- **Changed** - profiles requiring API calls (new or updated)
- **Unchanged** - profiles matching service state
- **NonExistent** - implicit profiles that don't exist in service
- **Invalid** - profiles failing validation
- **Replaced** - profiles that already existed in service and were adopted by Recyclarr (warning
  emitted)
- **Rename conflict** - cached profile rename blocked because the target name is occupied
- **Ambiguous** - multiple name matches

Config is authoritative: if a guide-backed profile is in the user's config and a single matching
name exists in the service, Recyclarr adopts it automatically. Rename conflicts (where the target
name is occupied by a different profile) still require manual resolution because the API cannot
merge two profiles.

## Future Work

**REC-26**: Full language configuration in YAML (currently passthrough only).
