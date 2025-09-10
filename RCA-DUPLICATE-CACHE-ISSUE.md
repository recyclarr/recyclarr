# Root Cause Analysis: Duplicate Cache ID Issue

## Problem Statement
The `radarr_develop` instance repeatedly updates 1 existing custom format on every sync run, alternating between "Remaster" and "4K Remaster" formats, even though no actual changes should be needed.

## Symptoms
- Every sync shows "Updated 1 Existing Custom Formats"
- Alternates between updating "Remaster" and "4K Remaster" 
- Both formats have same `custom_format_id: 3` in cache
- Should show "All custom formats are already up to date!" after first sync

## Root Cause Discovery

### Cache State Analysis
Cache contains duplicate entries for same ID:
```json
{
  "trash_id": "570bc9ebecd92723d2d21500f4be314c",
  "custom_format_name": "Remaster", 
  "custom_format_id": 3
},
{
  "trash_id": "eca37840c13c6ef2dd0262b141a5482f",
  "custom_format_name": "4K Remaster",
  "custom_format_id": 3
}
```

### Transaction Processing Flow
1. **Cache Lookup**: Both TrashIds return same ID (3)
2. **Service Matching**: 
   - "Remaster": Finds exact name match → ProcessExistingCf → UnchangedCustomFormats
   - "4K Remaster": Finds by ID but name mismatch → UpdatedCustomFormats
3. **Result**: Both assigned `Id = 3` but different transaction categories

### Critical Flaw in Cache.Update()
The `DistinctBy(x => x.CustomFormatId)` only deduplicates the LEFT side (existing cache), but the RIGHT side (transaction data) can still contain multiple CFs with same ID.

**Debug Evidence:**
```
[WRN] DistinctBy removed 1 duplicate mappings (37 -> 36)
[DBG]   Removed: eca37840c13c6ef2dd0262b141a5482f (4K Remaster) -> ID 3

[DBG] Cache state at Update - AFTER: 37 mappings  
[DBG]   - eca37840c13c6ef2dd0262b141a5482f (4K Remaster) -> ID 3
[DBG]   - 570bc9ebecd92723d2d21500f4be314c (Remaster) -> ID 3
```

**The FullOuterHashJoin re-adds the duplicate!**

### Service State Analysis (API Evidence)
API query to Radarr shows only **ONE** custom format exists:
```json
{
  "id": 3,
  "name": "4K Remaster",
  "specifications": [
    {"name": "Remaster", "value": "Remaster"},
    {"name": "4K", "value": "..."}
  ]
}
```

**Reality**: Only ID 3 = "4K Remaster" exists in service
**Cache Problem**: Two TrashIds both think they own ID 3
- `570bc9ebecd92723d2d21500f4be314c` → "Remaster" → ID 3 ❌
- `eca37840c13c6ef2dd0262b141a5482f` → "4K Remaster" → ID 3 ✅

### FullOuterHashJoin Logic Issue
```csharp
.FullOuterHashJoin(
    existingCfs,           // Contains BOTH "Remaster" and "4K Remaster" with Id=3
    l => l.CustomFormatId, // LEFT: Cache entries (after DistinctBy)
    r => r.Id,             // RIGHT: Transaction CFs (can have duplicates!)
    l => l,                // Keep existing service CFs  
    r => new TrashIdMapping(r.TrashId, r.Name, r.Id), // CREATE NEW - PROBLEM!
    (l, r) => l with { ... } // Update existing
)
```

When multiple RIGHT-side items have same key, join creates multiple output mappings.

## How Duplicates Originally Got Into Cache
Most likely scenarios:
1. **User manual rename**: User renamed "Remaster" to "4K Remaster" in Radarr UI, then added "4K Remaster" TrashId to config
2. **Previous version bug**: Earlier Recyclarr version allowed duplicates to persist
3. **Cache corruption**: Power loss or file system issue during cache write

## Current Expected vs Actual Flow

### Expected Flow (Your Original Analysis)
```
Sync Run 1: Cache has duplicates → DistinctBy removes duplicates → Save clean cache
Sync Run 2: Cache is clean → No more issues
```

### Actual Flow (Bug)
```
Sync Run 1: 
- Cache has duplicates
- Transaction processing assigns BOTH TrashIds same ID
- DistinctBy removes one duplicate from cache 
- FullOuterHashJoin processes BOTH transaction CFs with same ID
- Result: Duplicates restored in cache

Sync Run 2: Same cycle repeats
```

## Fix Strategies

### Strategy 1: Fix in RemoveStale() (Preventive)
Clean duplicates before transaction processing:
```csharp
public void RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
{
    // Remove duplicates intelligently based on service name matching
    // Keep the cache entry that matches actual service CF name
}
```

### Strategy 2: Fix in Update() (Defensive)  
Prevent transaction data from containing same ID multiple times:
```csharp
// Deduplicate transaction data before join
var existingCfs = transactions.AllCustomFormats
    .GroupBy(cf => cf.Id)
    .Select(g => g.First()) // Keep first occurrence per ID
```

### Strategy 3: Fix in Transaction Processing (Root Cause)
Prevent multiple guide CFs from getting same cached ID:
```csharp
// In CustomFormatTransactionPhase
// Validate that cache lookups don't return duplicate IDs
```

## Test Case Created
`Duplicate_mappings_are_resolved_by_service_name_match()` - Tests that RemoveStale() intelligently resolves duplicates by keeping the mapping that matches the actual service CF name.

### Test Execution Instructions

#### Run the failing test (Red phase):
```bash
dotnet test tests/Recyclarr.Cli.Tests/ --filter "Duplicate_mappings_are_resolved_by_service_name_match" --no-restore --logger "console;verbosity=normal"
```

#### Run existing duplicate removal test (should pass):
```bash
dotnet test tests/Recyclarr.Cli.Tests/ --filter "Duplicate_mappings_should_be_removed" --no-restore --logger "console;verbosity=normal"
```

#### Run with debug logging to reproduce issue:
```bash
dotnet run --project src/Recyclarr.Cli/ -- sync -d
```

#### Key log patterns to watch for:
- `[WRN] DUPLICATE ID 3: 2 entries` - Confirms duplicates exist
- `[WRN] DistinctBy removed 1 duplicate mappings` - Shows deduplication
- `Cache state at Update - AFTER: 37 mappings` - Shows duplicates restored
- `Updated 1 Existing Custom Formats` - Shows the recurring update issue

## Immediate Workaround
Manually delete one of the duplicate entries from:
`/Users/robert/Library/Application Support/recyclarr/cache/radarr/b4df30161e8a0512/custom-format-cache.json`

Remove either:
- `"trash_id": "570bc9ebecd92723d2d21500f4be314c"` (Remaster) 
- `"trash_id": "eca37840c13c6ef2dd0262b141a5482f"` (4K Remaster)

Keep the one that matches what actually exists in Radarr service.