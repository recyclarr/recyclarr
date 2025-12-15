# Custom Format Sync Redesign

## Status

Phase 1: COMPLETE - `recyclarr cache rebuild` command with UX improvements
Phase 2: COMPLETE - ID-first matching in sync pipeline

## Phase 1 Implementation

### Files Created/Modified

- `src/Recyclarr.Cli/Console/Settings/ICacheRebuildSettings.cs`
- `src/Recyclarr.Cli/Processors/CacheRebuild/ICacheRebuildProcessor.cs`
- `src/Recyclarr.Cli/Processors/CacheRebuild/CacheRebuildProcessor.cs`
- `src/Recyclarr.Cli/Console/Commands/CacheRebuildCommand.cs`
- `src/Recyclarr.Cli/CompositionRoot.cs` (registration)
- `src/Recyclarr.Cli/Console/CliSetup.cs` (cache branch)
- `tests/Recyclarr.Cli.Tests/IntegrationTests/CacheRebuild/CacheRebuildIntegrationTest.cs`
- `tests/Recyclarr.Core.TestLibrary/IntegrationTestFixture.cs` (IEnvironment mock fix)

### Test Cases

The test file `CacheRebuildIntegrationTest.cs` contains 11 tests using behavioral verification
(exit codes + cache file content), not console output assertions:

1. `Rebuild_matches_cfs_by_name_case_insensitive` - Verifies cache mappings for case-variant names
2. `Rebuild_detects_ambiguous_matches_and_fails` - Exit code 1, no cache file created
3. `Rebuild_only_caches_cfs_that_exist_in_service` - Cache contains only matched CFs
4. `Preview_mode_does_not_save_cache` - Cache unchanged after preview
5. `Rebuild_with_explicit_custom_formats_resource_type` - Cache created with explicit arg
6. `Rebuild_preserves_non_configured_cache_entries` - Both configured and non-configured in cache
7. `Rebuild_removes_stale_cache_entries` - Stale entry removed from cache
8. `Rebuild_corrects_cache_entries_with_wrong_service_id` - Cache contains corrected ID
9. `Rebuild_preserves_cache_entries_that_are_already_correct` - Cache unchanged

### Key Implementation Details

**CacheRebuildProcessor** uses:
- `ConfigurationScopeFactory` to create per-instance DI scopes
- `CustomFormatResourceQuery` to load guide CFs (injected directly, singleton)
- `ICustomFormatApiService` to fetch service CFs (resolved per-scope)
- `ICachePersister<CustomFormatCache>` to save rebuilt cache (resolved per-scope)
- `IAnsiConsole` for all user output (Rule, Grid, Table, Panel widgets)

**Matching logic (CORRECTED):**
- Iterates CONFIGURED CFs only (from user's YAML)
- For each configured CF, find matching service CF by name (case-insensitive)
- 0 matches = "new" (will be created on next sync, no cache entry yet)
- 1 match = add/update cache entry
- 2+ matches = ambiguous error

**Cache preservation:**
- Preserves non-configured cache entries (needed for sync deletion with
  `delete_old_custom_formats: true`)
- Removes stale entries where service CF no longer exists

**Cache states (`CfCacheState` enum):**

Cache changes (actions taken):
- `Added`: new cache entry created (CF found in service, not previously cached)
- `Corrected`: cache entry service_id updated
- `Removed`: stale entry deleted (service CF no longer exists)

Informational (no cache change):
- `NotInService`: configured CF not found in service (will be created on sync)
- `Unchanged`: cache entry already correct
- `Preserved`: non-configured entry kept for sync deletion
- `Ambiguous`: multiple service CFs match (error)

**Output format:**

Grouped into two sections:
1. **Changes**: Shows Added/Corrected/Removed counts, or "None - cache already correct"
2. **Summary**: Shows Unchanged/NotInService/Preserved/Ambiguous counts

Save message reflects actual changes:
- "Cache saved with N entries." (when HasChanges)
- "Cache unchanged (N entries)." (when no changes)

**Error handling:**
- Per-instance autonomy (failure in one doesn't block others)
- All ambiguous matches collected before reporting
- Exit code reflects any failures

## Critical Insight: Cache Scope

The cache maps trash_id to service_id for CFs that Recyclarr manages. Key understanding:

**Cache is scoped to CONFIGURED CFs, not all guide CFs.**

Why this matters:
- Sync's `delete_old_custom_formats: true` deletes CFs that are in cache but NOT in config
- If cache rebuild added all guide-matching CFs (not just configured), it would cause unintended
  deletions on next sync
- Cache rebuild must preserve non-configured entries so sync can delete them intentionally

**What the cache contains:**
- Entries for CFs that were synced via config (created or adopted)
- Does NOT contain manually-created CFs with no trash_id
- Does NOT contain guide CFs that aren't in user's config

**Cache rebuild must:**
- Iterate CONFIGURED CFs (from YAML), not all guide CFs
- Match configured CFs to service by name
- Preserve non-configured cache entries (for sync deletion)
- Remove stale entries (service CF no longer exists)

## Future Enhancement: Cache Rebuild Filtering

**Status:** Not started - revisit later

Cache rebuild currently processes ALL configured CFs. Add filtering options:

1. **By trash ID** - `--trash-id <id>` (repeatable) to target specific CFs
2. **By state category** - `--state <state>` to filter by `CfCacheState` (e.g., only process
   `Corrected` or `NotInService` entries)

**Context:** During Phase 2 planning, discovered that cache rebuild's name-first matching can
"correct" a cache entry away from a valid ID when the service CF was renamed. Filtering would let
users selectively rebuild specific entries without affecting others that may be correct but have
name mismatches.

## Background

Issue #672 revealed a fundamental design flaw in CF matching logic. User had two CFs with case-variant
names ("hulu" id 12, "HULU" id 79). Recyclarr's case-insensitive name-first matching picked the wrong
one, causing Sonarr to reject the update.

## Root Cause Analysis

### Current Matching Order (problematic)

1. Find by name (case-insensitive) - `FindServiceCfByName()`
2. If no name match, find by cached ID - `FindServiceCfById()`

### Why Name-First Was Chosen

- Introduced in `cffb8d78` (pipeline refactor, Jan 2023)
- Previous logic was ID-first, name-fallback (`4ae54d8f`)
- Name-first enables `replace_existing_custom_formats: true` to "adopt" manually-created CFs
- Assumption: Sonarr/Radarr enforce case-insensitive uniqueness (incorrect)

### The Problem

- Sonarr is case-SENSITIVE for uniqueness (`f.Name == c` in C#)
- Multiple case-variant CFs can coexist: "HULU", "Hulu", "hulu"
- Case-insensitive `FirstOrDefault` returns arbitrary match
- Recyclarr picks wrong CF, tries to rename it, Sonarr rejects

## Design Discussion (with nitsua, Notifiarr maintainer)

Key insights from nitsua's approach:

1. **ID is truth after creation** - name doesn't matter for matching
2. **Pre-sync ID reconciliation** - "auto unfucks ids" by remapping cache against service
3. **Name conflicts = error** - don't try to be clever, error out
4. **Philosophy** - stop chasing edge cases with auto-fixes

## Agreed Design Changes

### Phase 1: Cache Rebuild Command

New CLI command: `recyclarr cache rebuild`

Purpose: Explicitly rebuild cache by matching configured CFs to service CFs by name. Handles:
- Accidentally deleted cache
- Migrating to new instance
- Recovering from corruption
- Fixing incorrect cache mappings

Behavior:
1. Load existing cache
2. Fetch all CFs from service
3. Load configured CFs from user's YAML (not all guide CFs!)
4. For each CONFIGURED CF, find matching service CF by name (case-insensitive):
   - 0 matches = "new" (no cache entry - will be created on next sync)
   - 1 match = add/update cache entry
   - 2+ matches = ambiguous error (user must fix duplicates in service)
5. Preserve non-configured cache entries (needed for sync deletion)
6. Remove stale entries (service CF no longer exists)

Output grouped into two sections:

**Changes** (cache modifications):
- Added: new cache entries created
- Corrected: cache entries with updated service_id
- Removed: stale entries deleted (service CF no longer exists)

**Summary** (informational):
- Unchanged: cache entries already correct
- Not in service: configured CFs not in service (will be created on sync)
- Preserved: non-configured cache entries kept for sync deletion
- Ambiguous: configured CFs with multiple service matches (error)

CLI options:
- `-i`/`--instance`: Same semantics as sync (repeatable, omit for all instances)
- `-p`/`--preview`: Show what would change without saving
- Output via `IAnsiConsole` only (no Serilog for display)

### Phase 2: Sync Pipeline Simplification (COMPLETE)

Implemented ID-first matching in `CustomFormatTransactionPhase`:

**Algorithm:**
1. Cache entry exists + service CF with that ID exists → UPDATE (regardless of name)
2. Cache entry exists + service CF deleted (stale cache) → check name collision → CREATE or ERROR
3. No cache entry + name exists in service → ERROR (suggests `cache rebuild --adopt`)
4. No cache entry + name doesn't exist → CREATE

**Files modified:**
- `src/Recyclarr.Cli/Pipelines/CustomFormat/PipelinePhases/CustomFormatTransactionPhase.cs` - ID-first algorithm
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Models/AmbiguousMatch.cs` - moved from CacheRebuild (shared)
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Models/CustomFormatTransactionData.cs` - added AmbiguousCustomFormats
- `src/Recyclarr.Cli/Pipelines/CustomFormat/CustomFormatTransactionLogger.cs` - error messages for conflicts/ambiguous
- `src/Recyclarr.Core/Config/Parsing/PostProcessing/Deprecations/ReplaceExistingCfsDeprecationCheck.cs` - deprecation
- `src/Recyclarr.Core/CoreAutofacModule.cs` - registered deprecation check

**Behavioral changes:**
- `replace_existing_custom_formats` is now deprecated and a no-op
- Name collisions without cache entry produce errors instead of silent adoption
- Ambiguous matches (multiple case-variants) detected and reported
- Uses O(1) lookups (Dictionary/Lookup) instead of O(n) FirstOrDefault

## Key Files

### CLI Commands
- `src/Recyclarr.Cli/Console/Commands/SyncCommand.cs` - reference for -i option pattern
- `src/Recyclarr.Cli/Console/CliSetup.cs` - command registration

### Cache System
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/CustomFormatCache.cs`
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/CustomFormatCacheObject.cs`
- `src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/CustomFormatCachePersister.cs`
- `src/Recyclarr.Core/Cache/TrashIdCache.cs` - generic base
- `src/Recyclarr.Core/Cache/TrashIdMapping.cs` - mapping record
- `src/Recyclarr.Core/Cache/CacheStoragePath.cs` - path calculation

### CF Pipeline
- `src/Recyclarr.Cli/Pipelines/CustomFormat/PipelinePhases/CustomFormatApiFetchPhase.cs`
- `src/Recyclarr.Cli/Pipelines/CustomFormat/PipelinePhases/CustomFormatTransactionPhase.cs`
- `src/Recyclarr.Cli/Pipelines/CustomFormat/PipelinePhases/CustomFormatApiPersistencePhase.cs`

### Config/Instance Handling
- `src/Recyclarr.Cli/Processors/SyncProcessor.cs` - instance iteration pattern
- `src/Recyclarr.Core/Config/Filtering/ConfigFilterCriteria.cs` - instance filtering

## Code Share Opportunities

For cache rebuild command, reuse:
- `ConfigFilterCriteria` / `ConfigurationRegistry` for instance filtering
- `CustomFormatApiService.GetCustomFormats()` for fetching service CFs
- Config loading pipeline (to get CONFIGURED CFs, not all guide CFs)
- `CustomFormatCachePersister` for loading/saving cache
- `IAnsiConsole` patterns from existing commands

Key insight: Need to load configured CFs the same way sync does, not via raw
`CustomFormatResourceQuery` which returns ALL guide CFs.

## References

- Issue: https://github.com/recyclarr/recyclarr/issues/672
- Sonarr validation: case-sensitive `f.Name == c && f.Id != v.Id`
- Original name-first commit: `cffb8d78`
- ID-first restore commit: `4ae54d8f`
