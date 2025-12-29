# Trash ID Cache System

> Part of the [Sync Pipeline Architecture](sync-pipeline-architecture.md). For decision rationale,
> see [ADR-002](../decisions/002-id-first-custom-format-matching.md).

## Overview

The Trash ID Cache System provides ownership tracking for guide-backed resources (Custom Formats,
Quality Profiles) synchronized between TRaSH Guides and Sonarr/Radarr. It enables ID-first matching,
rename detection, and controlled deletion of managed resources.

## Problem Space

Synchronizing guide resources to Sonarr/Radarr presents several challenges:

**Identity Mismatch**: TRaSH Guides identify resources by `trash_id` (stable, globally unique),
while Sonarr/Radarr use numeric `service_id` (instance-specific, auto-generated). Without a mapping
layer, Recyclarr cannot reliably track which service resource corresponds to which guide resource.

**Name Collision**: Service APIs allow duplicate or near-duplicate names (e.g., case variants).
Name-based matching is unreliable and can cause "Must be unique" API errors.

**Ownership Tracking**: Recyclarr must distinguish between:

- Resources it created (safe to update/delete)
- Resources created manually by users (should not modify without explicit adoption)
- Resources removed from user's config (candidates for deletion)

## Cache Architecture

### Core Data Model

The cache stores `trash_id → service_id` mappings with name for diagnostics.

### Cache Storage

Cache files are stored per-instance in the app data directory. Each instance has its own cache
because service IDs are only meaningful within a single Sonarr/Radarr instance.

### Cache Scope: Configured Resources Only

**Critical invariant**: The cache only contains entries for resources that appear in the user's
effective configuration. This includes:

- Resources explicitly listed in YAML
- Resources derived from guide-backed Quality Profile `formatItems`

It does NOT contain:

- Manually-created resources with no trash_id
- Guide resources not referenced (directly or indirectly) in user's config
- User-defined resources (those without trash_id)

This scope restriction is essential for deletion features. When enabled, sync deletes resources that
are:

1. In the cache (Recyclarr owns them)
2. NOT in the current config (user removed them)

If the cache contained all guide resources, removing one from config would delete it even if the
user never intended to manage it.

## ID-First Matching Algorithm

The sync transaction phase uses an ID-first matching strategy that trusts cached IDs over name
matching.

### Decision Flow

```txt
Guide resource to sync
    ↓
Cache entry exists?
    ├─ Yes → Service resource with cached ID exists?
    │         ├─ Yes → UPDATE by ID
    │         └─ No  → Stale cache, check name collision
    └─ No  → Name exists in service?
              ├─ No match      → CREATE new
              ├─ Single match  → ERROR: name collision (suggest --adopt)
              └─ Multi match   → ERROR: ambiguous
```

### The Four Cases

#### Case 1: Cached ID exists + service resource exists → UPDATE

The cache provides a known-good ID. Update the service resource regardless of whether its name
matches the guide name. This handles legitimate renames.

#### Case 2: Cached ID exists + service resource deleted → check name collision

The cache references a resource that no longer exists. Fall through to name collision checking.

#### Case 3: No cache + name exists in service → ERROR

A resource with this name exists but Recyclarr doesn't own it. Error with suggestion to run `cache
rebuild --adopt`.

#### Case 4: No cache + no name match → CREATE

No ownership and no name conflict. Safe to create a new resource.

### Ambiguous Match Detection

When checking for name collisions, Recyclarr uses case-insensitive matching but counts all matches:

- 0 matches: safe to create
- 1 match: collision error, suggest adopt
- 2+ matches: ambiguous, user must resolve duplicates in service

## Cache Lifecycle

### During Sync

1. **Load**: Cache loaded in ApiFetch phase
2. **Match**: Transaction phase uses cache for ID-first matching
3. **Update**: After API persistence, cache refreshes mappings
4. **Save**: Persists updated cache

### Cache Rebuild Command

The `cache rebuild` command provides explicit cache reconstruction when missing, corrupted, or
needing correction.

**Use cases**:

- Migration to new machine/instance
- Recovery from cache corruption
- Adopting manually-created resources
- Fixing incorrect mappings

**Matching behavior** (name-first, inverse of sync):

1. Load effective configuration (including QP-derived CFs)
2. Fetch all resources from service
3. Match by name (case-insensitive)
4. Create/update cache entries for matches

**The `--adopt` flag**: By default, rebuild only updates entries for previously-owned resources.
`--adopt` extends this to take ownership of untracked resources that match by name.

## Design Principles

**Explicit Adoption**: Never silently take ownership of existing resources. Require explicit
`--adopt` flag or manual cache intervention.

**ID Stability**: Once cached, a trash_id → service_id mapping persists across renames. The guide's
trash_id is the stable identifier.

**Graceful Degradation**: Missing or corrupted cache results in errors with clear remediation steps,
not silent data corruption.

**Scoped Ownership**: Cache only tracks what the user configures (directly or via QP formatItems).
Unconfigured resources remain untouched.

## Cache Invariants

**Unique Service IDs**: Each service_id must appear in at most one cache entry. Two trash_ids
mapping to the same service_id is invalid - it implies two different guide resources own the same
service resource, which is impossible.

**Immutable Trash IDs**: Trash IDs are stable identifiers from TRaSH Guides. They never change once
assigned to a custom format or quality profile definition.

## Responsibility Split: Sync vs Cache Rebuild

The sync pipeline and cache rebuild command have distinct responsibilities:

**Sync Pipeline**: Simple and picky. Assumes the cache is valid. If it detects cache inconsistency
(e.g., duplicate service IDs causing conflicting update+delete), it errors and tells the user to run
`cache rebuild`. Sync does NOT attempt to fix cache problems.

**Cache Rebuild**: The repair tool. Reconstructs cache from current config + service state.
Deduplicates by service ID (keeps config-backed entry, discards orphans). Produces a clean,
consistent cache.

## Implementation

### Class Structure

```txt
ICacheSyncSource (interface in Core)
  ├── SyncedMappings: IEnumerable<TrashIdMapping>  // New + Updated + Unchanged resources
  ├── DeletedIds: IEnumerable<int>                  // Service IDs to remove from cache
  └── ValidServiceIds: IEnumerable<int>             // All IDs currently in service

TrashIdCache<TCacheObject> (concrete class in Core)
  ├── Mappings: IReadOnlyList<TrashIdMapping>
  ├── FindId(string trashId): int?
  └── Update(ICacheSyncSource source): void

ICachePersister<TCacheObject> (interface in Core)
  ├── Load(): TrashIdCache<TCacheObject>
  └── Save(TrashIdCache<TCacheObject> cache): void

CachePersister<TCacheObject> (abstract base in Core)
  └── Requires: abstract CacheName property for logging

CustomFormatCachePersister : CachePersister<CustomFormatCacheObject>
QualityProfileCachePersister : CachePersister<QualityProfileCacheObject>
```

### ICacheSyncSource Integration

Pipeline context objects implement `ICacheSyncSource` directly because they already aggregate
`TransactionOutput` and `ApiFetchOutput`. This provides a clean interface for cache updates:

```csharp
// In persistence phase:
cache.Update(context);  // context implements ICacheSyncSource
cachePersister.Save(cache);
```

**CF Implementation**: `SyncedMappings` combines New/Updated/Unchanged CFs. `DeletedIds` comes from
`DeletedCustomFormats`.

**QP Implementation**: `SyncedMappings` combines New/Updated/Unchanged profiles (filtering to those
with trash_id). `DeletedIds` is empty (QP has no delete feature).

### Cache Update Semantics

The `TrashIdCache.Update()` method merges synced mappings with existing cache state:

1. Filter existing mappings to valid entries (exist in service, not deleted)
2. Full outer join with synced mappings on service ID
3. Keep existing entries not in user config (supports delete feature)
4. Add new mappings from user config
5. Update existing mappings with current trash_id/name

This produces a cache that tracks service state, not config state. Entries persist as long as the
service resource exists, enabling the delete feature to clean up resources removed from config.
