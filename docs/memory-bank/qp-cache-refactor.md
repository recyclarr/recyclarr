# QP Cache Refactor - Work in Progress

## Branch

`quality-profiles-json`

## Summary

Refactoring QualityProfile pipeline to align with CustomFormat pipeline patterns, focusing on
transaction data structure and cache semantics.

## Completed Changes

### Transaction Data Restructure

Changed `QualityProfileTransactionData` from `ChangedProfiles`/`UnchangedProfiles` split by
`HasChanges` to explicit collections matching CF pattern:

- `NewProfiles: Collection<UpdatedQualityProfile>` - profiles being created
- `UpdatedProfiles: Collection<ProfileWithStats>` - existing profiles with changes (stats for
  logging)
- `UnchangedProfiles: Collection<UpdatedQualityProfile>` - existing profiles without changes

### Removed UpdateReason Enum

- Deleted `QualityProfileUpdateReason` enum (New/Changed)
- Collection membership now indicates reason (matching CF pattern)
- Validator uses `ProfileDto.Id is null` to detect new profiles

### ProfileWithStats Simplified

- Removed `HasChanges` property (redundant once split by collection)
- Kept `ProfileChanged`, `ScoresChanged`, `QualitiesChanged` for logging granularity
- Changed to `init` setters for immutability

### Updated Pipeline Phases

- `UpdatedProfileBuilder`: Adds new profiles directly to `transactions.NewProfiles`, returns
  existing profiles
- `QualityProfileTransactionPhase`: Processes new/existing separately, calculates stats for
  existing
- `QualityProfileApiPersistencePhase`: Iterates `NewProfiles` for creation, `UpdatedProfiles` for
  updates
- `QualityProfilePreviewPhase`: Uses new collections with explicit change reason strings
- `QualityProfileLogger`: Uses new collections, no more filtering by UpdateReason

### Cache Architecture Refactor (Completed)

Unified CF and QP caches under a single generic implementation with shared semantics.

**New Architecture:**

- `ICacheSyncSource` interface: Defines `SyncedMappings`, `DeletedIds`, `ValidServiceIds` for cache
  updates
- `TrashIdCache<TCacheObject>`: Generic cache with public `FindId(string)` and
  `Update(ICacheSyncSource)`
- `ICachePersister<TCacheObject>`: Generic persister returning `TrashIdCache<TCacheObject>`
- Pipeline contexts implement `ICacheSyncSource` directly (CF and QP)

**Deleted Classes:**

- `CustomFormatCache` - replaced by `TrashIdCache<CustomFormatCacheObject>`
- `QualityProfileCache` - replaced by `TrashIdCache<QualityProfileCacheObject>`

**Key Design Decisions:**

1. Context objects implement `ICacheSyncSource` because they already aggregate TransactionOutput and
   ApiFetchOutput
2. Cache semantics follow CF model: entries removed only when service ID no longer exists, not when
   removed from config
3. QP no longer has "stale entry" logic that incorrectly removed entries when profiles left config
4. Single `Update(ICacheSyncSource)` method replaces per-pipeline Update overloads

**Interface Responsibilities:**

```csharp
interface ICacheSyncSource
{
    IEnumerable<TrashIdMapping> SyncedMappings { get; }  // New + Updated + Unchanged
    IEnumerable<int> DeletedIds { get; }                  // CF: from DeletedCustomFormats, QP: empty
    IEnumerable<int> ValidServiceIds { get; }             // From ApiFetchOutput
}
```

**Files Modified for Cache Refactor:**

- `src/Recyclarr.Core/Cache/ICachePersister.cs` - simplified interface
- `src/Recyclarr.Core/Cache/ICacheSyncSource.cs` - new interface
- `src/Recyclarr.Core/Cache/TrashIdCache.cs` - public FindId, Update(ICacheSyncSource)
- `src/Recyclarr.Core/Cache/CachePersister.cs` - single generic parameter
- `src/Recyclarr.Cli/Pipelines/CustomFormat/CustomFormatPipelineContext.cs` - implements
  ICacheSyncSource
- `src/Recyclarr.Cli/Pipelines/QualityProfile/QualityProfilePipelineContext.cs` - implements
  ICacheSyncSource
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/CustomFormatCachePersister.cs` - simplified
- `src/Recyclarr.Cli/Pipelines/QualityProfile/Cache/QualityProfileCachePersister.cs` - simplified
- Pipeline phases updated to use new types and `cache.Update(context)` pattern

**Files Deleted:**

- `src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/CustomFormatCache.cs`
- `src/Recyclarr.Cli/Pipelines/QualityProfile/Cache/QualityProfileCache.cs`
- `src/Recyclarr.Cli/Pipelines/QualityProfile/Models/QualityProfileUpdateReason.cs`

## Remaining Work

None - all refactoring complete and tests passing.

## Architecture Reference

See `docs/architecture/trash-id-cache-system.md` for cache conceptual model and matching algorithm.
The implementation section below documents the authoritative class structure.

### Authoritative Cache Class Structure

```txt
ICacheSyncSource (interface in Core)
  ├── SyncedMappings: IEnumerable<TrashIdMapping>
  ├── DeletedIds: IEnumerable<int>
  └── ValidServiceIds: IEnumerable<int>

TrashIdCache<TCacheObject> (concrete class in Core)
  ├── Mappings: IReadOnlyList<TrashIdMapping>
  ├── FindId(string trashId): int?
  └── Update(ICacheSyncSource source): void

ICachePersister<TCacheObject> (interface in Core)
  ├── Load(): TrashIdCache<TCacheObject>
  └── Save(TrashIdCache<TCacheObject> cache): void

CachePersister<TCacheObject> (abstract class in Core)
  ├── Implements ICachePersister<TCacheObject>
  └── Requires: abstract CacheName property

CustomFormatCachePersister : CachePersister<CustomFormatCacheObject>
QualityProfileCachePersister : CachePersister<QualityProfileCacheObject>

CustomFormatPipelineContext : PipelineContext, ICacheSyncSource
QualityProfilePipelineContext : PipelineContext, ICacheSyncSource
```

### Cache Update Flow

```txt
1. ApiFetch phase: cache = cachePersister.Load()
2. Transaction phase: uses cache.FindId() for ID-first matching
3. Persistence phase: cache.Update(context)  // context implements ICacheSyncSource
4. Persistence phase: cachePersister.Save(cache)
```
