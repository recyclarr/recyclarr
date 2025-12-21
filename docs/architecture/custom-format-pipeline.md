# Custom Format Sync

Custom Format synchronization follows the standard pipeline pattern. This document covers
CF-specific behaviors in transaction and persistence phases. For plan phase details, see
[sync-plan-phase.md](sync-plan-phase.md). For the ID-first matching algorithm, see
[trash-id-cache-system.md](trash-id-cache-system.md).

## Case Sensitivity

Sonarr uses case-sensitive comparison for CF name uniqueness validation. "HULU", "Hulu", and "hulu"
can coexist as separate CFs. Case-insensitive name matching returns an arbitrary match when variants
exist, causing "Must be unique" API errors.

The cache system's ID-first matching solves this by using cached service IDs rather than names.

## Delete Old Custom Formats

The `delete_old_custom_formats` config option enables automatic cleanup of CFs that:

1. Are in the cache (Recyclarr owns them)
2. Are NOT in the current config (user removed them)
3. Still exist in the service

This is CF-specific. Quality Profiles do not support deletion due to the consequences for media
organization.

## CF Equivalence

Two CFs are equivalent if they have matching:

- ID and Name
- `IncludeCustomFormatWhenRenaming` flag
- Specifications (compared by name)

Non-equivalent CFs with the same cached ID trigger an update.

## Transaction Categories

- **New** - CFs to create (no cache, no name collision)
- **Updated** - CFs to update (found by cached ID, content differs)
- **Unchanged** - CFs matching service state
- **Deleted** - CFs to delete (in cache, not in config, `delete_old` enabled)
- **Conflicting** - Name collision (suggest `--adopt`)
- **Ambiguous** - Multiple name matches (user must resolve)

## Cache Rebuild States

CF cache rebuild produces these states:

**Changes**: Added, Adopted, Corrected, Removed

**Informational**: NotInService, Unchanged, Preserved, Ambiguous

The `Preserved` state keeps entries for deleted-from-config CFs, enabling the `delete_old` feature
to clean them up on next sync.
