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

**Ownership Tracking**: Recyclarr must track which service resources it manages to support deletion
of resources removed from config.

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

Config is authoritative: if a resource is in the user's YAML, Recyclarr owns it. Single name matches
are adopted automatically; only ambiguous matches (2+ service resources with the same name) require
manual resolution.

```txt
Guide resource to sync
    ↓
State entry exists?
    ├─ Yes → Service resource with stored ID exists?
    │         ├─ Yes → UPDATE by ID
    │         └─ No  → Stale state, check name collision
    └─ No  → Name exists in service?
              ├─ No match      → CREATE new
              ├─ Single match  → ADOPT and UPDATE (warning emitted)
              └─ Multi match   → ERROR: ambiguous
```

### The Four Cases

#### Case 1: Stored ID exists + service resource exists -> UPDATE

The state provides a known-good ID. Update the service resource regardless of whether its name
matches the guide name. This handles legitimate renames.

#### Case 2: Stored ID exists + service resource deleted -> check name collision

The state references a resource that no longer exists. Fall through to name collision checking (same
logic as Case 3/4).

#### Case 3: No state + name exists in service -> ADOPT

A resource with this name exists. Since config is authoritative, Recyclarr adopts it and emits a
warning. The resource is updated with guide data going forward.

#### Case 4: No state + no name match -> CREATE

No existing resource. Safe to create a new one.

### Ambiguous Match Detection

When checking for name collisions, Recyclarr uses case-insensitive matching but counts all matches:

- 0 matches: safe to create
- 1 match: adopt automatically (warning emitted)
- 2+ matches: ambiguous, user must resolve duplicates in service

## State Lifecycle

### During Sync

1. **Load**: State loaded in ApiFetch phase
2. **Match**: Transaction phase uses state for ID-first matching
3. **Update**: After API persistence, state refreshes mappings
4. **Save**: Persists updated state

### State Repair Command (Deprecated)

The `state repair` command is deprecated. Sync now handles all state reconciliation automatically by
adopting existing resources that match by name. The command outputs a deprecation warning and exits.

## Design Principles

**Config is Authoritative**: If a resource is in the user's YAML config, Recyclarr owns it. Single
name matches are adopted automatically with a warning. Users who want to manually manage a resource
should remove it from their config.

**ID Stability**: Once stored, a trash_id to service_id mapping persists across renames. The guide's
trash_id is the stable identifier.

**Graceful Degradation**: Ambiguous matches (2+ service resources with the same name) produce errors
with clear remediation steps.

**Scoped Ownership**: State only tracks what the user configures (directly or via QP formatItems).
Unconfigured resources remain untouched.

## State Invariants

**Unique Service IDs**: Each service_id must appear in at most one state entry. Two trash_ids
mapping to the same service_id is invalid - it implies two different guide resources own the same
service resource, which is impossible.

**Immutable Trash IDs**: Trash IDs are stable identifiers from TRaSH Guides. They never change once
assigned to a custom format or quality profile definition.

## Self-Healing Sync

The sync pipeline is self-healing. It handles state inconsistencies inline rather than requiring a
separate repair step:

- **Stale IDs**: When a stored service ID no longer exists, sync falls through to name-based
  matching and adopts automatically.
- **Missing state**: When no state entry exists but a matching resource is in the service, sync
  adopts it (single match) or errors (ambiguous match).
- **Duplicate service IDs**: Detected and reported as invalid cache entries; cleaned up during state
  update.

## Implementation

### Class Structure

```txt
ISyncStateSource (interface in Core)
  ├── SyncedMappings: IEnumerable<TrashIdMapping>  // New + Updated + Unchanged resources
  ├── DeletedIds: IEnumerable<int>                  // Service IDs to remove from state
  └── ValidServiceIds: IEnumerable<int>             // All IDs currently in service

TrashIdMappingStore<TMappings> (concrete class in Core)
  ├── Mappings: IReadOnlyList<TrashIdMapping>
  ├── FindId(string trashId): int?                  // 1:1 lookup (CFs)
  ├── FindId(string trashId, string name): int?     // Composite lookup (QPs)
  ├── FindAllByTrashId(string trashId): IReadOnlyList<TrashIdMapping>
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
