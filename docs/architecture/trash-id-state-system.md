# Trash ID State System

> Part of the [Sync Pipeline Architecture](sync-pipeline-architecture.md). For decision rationale,
> see [ADR-002](../decisions/architecture/002-id-first-custom-format-matching.md).

## Overview

The Trash ID State System provides ownership tracking for guide-backed resources (Custom Formats,
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

## State Architecture

### Core Data Model

The state stores `trash_id → service_id` mappings with name for diagnostics.

### State Storage

State files are stored per-instance in the app data directory. Each instance has its own state
because service IDs are only meaningful within a single Sonarr/Radarr instance.

### State Scope: Configured Resources Only

**Critical invariant**: The state only contains entries for resources that appear in the user's
effective configuration. This includes:

- Resources explicitly listed in YAML
- Resources derived from guide-backed Quality Profile `formatItems`

It does NOT contain:

- Manually-created resources with no trash_id
- Guide resources not referenced (directly or indirectly) in user's config
- User-defined resources (those without trash_id)

This scope restriction is essential for deletion features. When enabled, sync deletes resources that
are:

1. In the state (Recyclarr owns them)
2. NOT in the current config (user removed them)

If the state contained all guide resources, removing one from config would delete it even if the
user never intended to manage it.

## ID-First Matching Algorithm

The sync transaction phase uses an ID-first matching strategy that trusts stored IDs over name
matching.

### Decision Flow

```txt
Guide resource to sync
    ↓
State entry exists?
    ├─ Yes → Service resource with stored ID exists?
    │         ├─ Yes → UPDATE by ID
    │         └─ No  → Stale state, check name collision
    └─ No  → Name exists in service?
              ├─ No match      → CREATE new
              ├─ Single match  → ERROR: name collision (suggest --adopt)
              └─ Multi match   → ERROR: ambiguous
```

### The Four Cases

#### Case 1: Stored ID exists + service resource exists → UPDATE

The state provides a known-good ID. Update the service resource regardless of whether its name
matches the guide name. This handles legitimate renames.

#### Case 2: Stored ID exists + service resource deleted → check name collision

The state references a resource that no longer exists. Fall through to name collision checking.

#### Case 3: No state + name exists in service → ERROR

A resource with this name exists but Recyclarr doesn't own it. Error with suggestion to run `state
repair --adopt`.

#### Case 4: No state + no name match → CREATE

No ownership and no name conflict. Safe to create a new resource.

### Ambiguous Match Detection

When checking for name collisions, Recyclarr uses case-insensitive matching but counts all matches:

- 0 matches: safe to create
- 1 match: collision error, suggest adopt
- 2+ matches: ambiguous, user must resolve duplicates in service

## State Lifecycle

### During Sync

1. **Load**: State loaded in ApiFetch phase
2. **Match**: Transaction phase uses state for ID-first matching
3. **Update**: After API persistence, state refreshes mappings
4. **Save**: Persists updated state

### State Repair Command

The `state repair` command provides explicit state reconstruction when missing, corrupted, or
needing correction.

**Use cases**:

- Migration to new machine/instance
- Recovery from state corruption
- Adopting manually-created resources
- Fixing incorrect mappings

**Matching behavior** (name-first, inverse of sync):

1. Load effective configuration (including QP-derived CFs)
2. Fetch all resources from service
3. Match by name (case-insensitive)
4. Create/update state entries for matches

**The `--adopt` flag**: By default, repair only updates entries for previously-owned resources.
`--adopt` extends this to take ownership of untracked resources that match by name.

## Design Principles

**Explicit Adoption**: Never silently take ownership of existing resources. Require explicit
`--adopt` flag or manual state intervention.

**ID Stability**: Once stored, a trash_id → service_id mapping persists across renames. The guide's
trash_id is the stable identifier.

**Graceful Degradation**: Missing or corrupted state results in errors with clear remediation steps,
not silent data corruption.

**Scoped Ownership**: State only tracks what the user configures (directly or via QP formatItems).
Unconfigured resources remain untouched.

## State Invariants

**Unique Service IDs**: Each service_id must appear in at most one state entry. Two trash_ids
mapping to the same service_id is invalid - it implies two different guide resources own the same
service resource, which is impossible.

**Immutable Trash IDs**: Trash IDs are stable identifiers from TRaSH Guides. They never change once
assigned to a custom format or quality profile definition.

## Responsibility Split: Sync vs State Repair

The sync pipeline and state repair command have distinct responsibilities:

**Sync Pipeline**: Simple and picky. Assumes the state is valid. If it detects state inconsistency
(e.g., duplicate service IDs causing conflicting update+delete), it errors and tells the user to run
`state repair`. Sync does NOT attempt to fix state problems.

**State Repair**: The repair tool. Reconstructs state from current config + service state.
Deduplicates by service ID (keeps config-backed entry, discards orphans). Produces a clean,
consistent state.

## Implementation

### Class Structure

```txt
ISyncStateSource (interface in Core)
  ├── SyncedMappings: IEnumerable<TrashIdMapping>  // New + Updated + Unchanged resources
  ├── DeletedIds: IEnumerable<int>                  // Service IDs to remove from state
  └── ValidServiceIds: IEnumerable<int>             // All IDs currently in service

TrashIdMappingStore<TMappings> (concrete class in Core)
  ├── Mappings: IReadOnlyList<TrashIdMapping>
  ├── FindId(string trashId): int?
  └── Update(ISyncStateSource source): void

ISyncStatePersister<TMappings> (interface in Core)
  ├── Load(): TrashIdMappingStore<TMappings>
  └── Save(TrashIdMappingStore<TMappings> state): void

SyncStatePersister<TMappings> (abstract base in Core)
  └── Requires: abstract StateName property for logging

CustomFormatStatePersister : SyncStatePersister<CustomFormatMappings>
QualityProfileStatePersister : SyncStatePersister<QualityProfileMappings>
```

### ISyncStateSource Integration

Pipeline context objects implement `ISyncStateSource` directly because they already aggregate
`TransactionOutput` and `ApiFetchOutput`. This provides a clean interface for state updates:

```csharp
// In persistence phase:
state.Update(context);  // context implements ISyncStateSource
statePersister.Save(state);
```

**CF Implementation**: `SyncedMappings` combines New/Updated/Unchanged CFs. `DeletedIds` comes from
`DeletedCustomFormats`.

**QP Implementation**: `SyncedMappings` combines New/Updated/Unchanged profiles (filtering to those
with trash_id). `DeletedIds` is empty (QP has no delete feature).

### State Update Semantics

The `TrashIdMappingStore.Update()` method merges synced mappings with existing state:

1. Filter existing mappings to valid entries (exist in service, not deleted)
2. Full outer join with synced mappings on service ID
3. Keep existing entries not in user config (supports delete feature)
4. Add new mappings from user config
5. Update existing mappings with current trash_id/name

This produces a state that tracks service state, not config state. Entries persist as long as the
service resource exists, enabling the delete feature to clean up resources removed from config.
