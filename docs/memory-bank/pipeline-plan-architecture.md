# Pipeline Plan Architecture

## RESUME POINT (2025-12-06)

**Status:** Implementation complete. All tests passing. Ready for commit.

### Session Summary (2025-12-06)

All remaining items from 2025-12-05 Session 2 have been addressed:

- Tests fixed (270 Cli + 148 Core passing)
- Removed unused properties: `PlannedQualityProfile.TrashId`, `PlannedCustomFormat.DefaultScore`
- Removed `UpdatedQualityItem.Formatted*` properties (rendering moved to Preview)
- Added exception message text to `PipelinePlan` throwing getters
- Fixed Plan access past Transaction: `context.QualityDefinitionType` set in Transaction phase

**Future Work (not blocking):**

- Rename `ApiFetch` phase to broader name (e.g., `DataLoad`)
- Consider similar UpdatedQualityItem pattern for MediaNaming if needed

---

### Previous Sessions

---

## Architectural Invariants

### Plan Phase Purpose

The Plan phase correlates and normalizes data into a domain-specific model. It is NOT for making
behavioral choices.

**Plan phase MUST:**
- Transform config + guide data into normalized plan structures
- Collect diagnostics (errors, warnings, invalid items)
- Ensure nested properties are non-null when parent is non-null

**Plan phase MUST NOT:**
- Make early-out/short-circuit decisions
- Log user-visible messages (Info/Warning/Error) - collect in diagnostics instead
- Block other pipelines due to feature-specific validation failures

**Plan phase MAY:**
- Use Debug-level logging for code flow tracing (supports RCA, not user-facing)

### ShouldProceed Semantics

`PlanDiagnostics.ShouldProceed` should only return `false` for truly fatal errors that affect ALL
pipelines (e.g., fundamental config/guide data corruption). Feature-specific validation failures
(invalid naming formats, unknown trash_ids) should be logged but NOT block other pipelines.

Current implementation: `ShouldProceed => _errors.Count == 0`

Only `AddError()` affects `ShouldProceed`. Other diagnostics (`InvalidTrashIds`, `InvalidNamingFormats`,
`Warnings`) are informational only.

### Plan Property Semantics (Two Layers)

**Top-level plan properties** signal "is this feature configured at all?":
- Collection (CF, QP): Empty collection = not configured / no work
- Object (QS, MN): Null = not configured / no work

**Nested properties** signal "how is this feature configured?":
- Follow their own semantics based on feature logic
- Plan guarantees non-null when parent is non-null

Example:
- `plan.QualitySizes == null` → User didn't configure `quality_definition`
- `plan.QualitySizes.Qualities` → How they configured it (may be empty if guide has no matching type)

### Pipeline Early-Out Responsibility

Decisions to terminate a pipeline belong to the pipeline itself, not to Plan or ShouldProceed.

Pattern for object-based plan properties:
```csharp
// In Transaction phase
var planned = context.Plan.QualitySizes;
if (planned is null)
{
    return PipelineFlow.Terminate;  // Feature not configured
}
// Process planned data...
```

Pattern for collection-based plan properties:
```csharp
// In Transaction phase
foreach (var cf in context.Plan.CustomFormats)  // Empty = 0 iterations
{
    // Process...
}
```

---

**Phase 4 Design Decisions:**

### QS Pipeline Refactoring

The deleted `QualitySizeConfigPhase` had logic that requires API data (limits). New design:

**Plan Phase (QualitySizePlanComponent):**
- Validate config type exists in guide
- Validate quality override names exist in guide
- Merge guide values + user overrides → flat list of "final" values
- Store `preferred_ratio` for Transaction to apply
- Output: `PlannedQualitySizes` with merged quality values

**Key Decision: Use `decimal?` for size values**
- `null` = unlimited (matches API contract - API sends `null` for unlimited)
- Numeric value = specific size
- Cleaner than string parsing, type-safe, matches API directly

**ApiFetch Phase:**
- Fetch quality definitions from service API
- Fetch quality item limits from service API (moved from old ConfigPhase)

**Transaction Phase:**
- Read merged qualities from `context.Plan.QualitySizes`
- Resolve `null` → actual limit value using fetched limits
- Apply ratio interpolation using limits
- Validate min ≤ preferred ≤ max (after resolution)
- Compare with API definitions, generate change list

**API Contract Reference:**
```csharp
// QualityItemWithLimits.cs - how unlimited is sent to API
public decimal? MaxForApi => Item.Max < Limits.MaxLimit ? Item.Max : null;
```

### CF Pipeline Refactoring
- Transaction reads from `context.Plan.CustomFormats` instead of `context.ConfigOutput`
- Cache loading moves to Transaction phase (first phase that needs it)

### PlannedQualitySizes Updates Needed
```csharp
internal class PlannedQualitySizes
{
    public required string Type { get; init; }
    public decimal? PreferredRatio { get; init; }
    public required IReadOnlyCollection<PlannedQualityItem> Qualities { get; init; }
}

internal record PlannedQualityItem(
    string Quality,      // quality name
    decimal Min,         // always numeric (no unlimited for min)
    decimal? Max,        // null = unlimited
    decimal? Preferred   // null = unlimited
);
```

**Implementation order:**
1. Update `PlannedQualitySizes` with new structure
2. Update `QualitySizePlanComponent` to populate merged qualities
3. Update `QualitySizeApiFetchPhase` to also fetch limits
4. Update `QualitySizeTransactionPhase` to read from plan + resolve limits
5. Update CF Transaction phase to read from `context.Plan.CustomFormats`
6. Move cache loading to CF Transaction phase
7. Remove `context.ConfigOutput` and related unused types
8. Fix tests

---

Finalized design for refactoring sync pipelines to support CF Groups and profile-by-trash_id.

## Problem Statement

CF Groups create a chicken-and-egg dependency:

- CF Pipeline needs to know which CFs to sync
- CF Group membership is determined by profile selection
- Profile selection happens in QP Pipeline, which runs AFTER CF Pipeline

## Solution: Two-Pass Architecture

```
PASS 1: PLAN PHASE
──────────────────
ServiceConfiguration (user YAML) + Guide Resources
                    │
                    ▼
            ┌──────────────┐
            │ PlanBuilder  │
            └──────────────┘
                    │
        ┌───────────┴───────────┐
        ▼                       ▼
┌──────────────┐        ┌──────────────┐
│ PipelinePlan │        │  Diagnostics │
│  (stateful)  │        │  (errors,    │
│              │        │   warnings)  │
└──────────────┘        └──────────────┘


PASS 2: SYNC PHASE
──────────────────
SyncService orchestrates:

1. Plan + Diagnostics = PlanBuilder.Build(config, resources)
2. DiagnosticsReporter.Report(diagnostics)
3. if (!diagnostics.ShouldProceed) return
4. Pass plan to pipelines:

CF Pipeline: [Fetch] → [Transaction] → [Persist]
                                           │
                              Hydrate plan with CF service IDs
                                           │
                                           ▼
QP Pipeline: [Fetch] → [Transaction] → [Persist]
                           │
                           └── reads IDs from plan

QS Pipeline: [Fetch] → [Transaction] → [Persist]
MN Pipeline: [Fetch] → [Transaction] → [Persist]
```

## PipelinePlan Class

Stateful class (not a record/DTO) that owns sync state centrally. Passed by reference through
pipelines.

### Responsibilities

- Store hierarchical resolved data (profiles → CFs, quality items, scores)
- Provide query methods for phases to access data
- Support mutation for ID hydration after CF Persistence

### Conceptual Structure

```
PipelinePlan
├── CustomFormats[]
│   └── TrashId, Name, TrashScores, DefaultScore, ServiceId (mutable)
│
├── QualityProfiles[]
│   ├── Name, TrashId
│   ├── QualityItems[] (from guide JSON)
│   └── CfScores[] (trash_id + score, references CFs above)
│
├── QualitySizes (flat, from guide + config overrides)
│
└── MediaNaming (flat, from guide + config overrides)
```

### Key Methods (Conceptual)

```csharp
// Query methods
IReadOnlyList<PlannedCustomFormat> GetCustomFormats();
IReadOnlyList<PlannedQualityProfile> GetQualityProfiles();
PlannedQualityProfile? GetProfileByTrashId(string trashId);

// Hydration (called by CF Persistence phase)
void HydrateCfServiceId(string trashId, int serviceId);
```

### "Resolved" Means Pre-Joined

Before (user config - disconnected references):

```yaml
quality_profiles:
  - trash_id: sqp-1
custom_formats:
  - trash_ids: [aaa]
    assign_scores_to:
      - name: SQP-1
```

After (PipelinePlan - relationships joined):

```
Profile "SQP-1":
  ├── CfScores: [{aaa, 500}, {xxx, 1000}, {yyy, 250}]
  │             (merged: explicit config + CF Groups)
  └── QualityItems: [Bluray-1080p, WEB-1080p, ...]
                    (from guide JSON)
```

## Diagnostics Pattern

Plan phase produces two outputs: the plan object and diagnostics. Plan components never log
directly - they collect issues in diagnostics.

### PlanDiagnostics Structure

```csharp
class PlanDiagnostics
{
    List<string> InvalidTrashIds { get; }
    List<InvalidNamingConfig> InvalidNamingFormats { get; }
    List<string> Warnings { get; }

    bool ShouldProceed => /* computed from error severity */;
}
```

### Benefits

- Plan components are pure (no logging side effects, easier to test)
- Single decision point for go/no-go
- Consolidated logging (all errors/warnings reported together)
- Unified error handling across all plan components

### Current Inconsistent Patterns Being Replaced

| Current Location | Pattern | Terminates? |
|------------------|---------|-------------|
| CF ConfigPhase | Stores `InvalidFormats` → logged later | No |
| MN ConfigPhase | Logs inline, then terminates | Yes |
| QP TransactionPhase | Stores `NonExistentProfiles` → logged later | No |

New pattern: All validation happens in Plan phase, diagnostics decide termination.

## Context Propagation

Plan flows through pipelines via context objects.

### Context Hierarchy

```
IPipelineContext
       │
       ▼
BaseContext (handles plan reference)
       │
       ├── CustomFormatPipelineContext
       ├── QualityProfilePipelineContext
       ├── QualitySizePipelineContext
       └── MediaNamingPipelineContext
```

### Flow

1. SyncService creates plan via PlanBuilder
2. SyncService passes plan to pipeline composite
3. Pipeline constructs context with plan reference
4. Context passed to all phases (existing pattern)
5. Phases access plan via `context.Plan`

## ID Hydration

New CFs don't have service IDs until created. Hydration updates the plan after CF Persistence.

### Location

Hydration happens inside CF Persistence phase (keeps it contained):

```csharp
// In CustomFormatApiPersistencePhase
foreach (var cf in transactions.NewCustomFormats)
{
    var response = await api.CreateCustomFormat(cf, ct);
    if (response is not null)
    {
        cf.Id = response.Id;
        context.Plan.HydrateCfServiceId(cf.TrashId, response.Id);
    }
}
```

### Why This Works

- Plan is shared reference across all pipeline contexts
- CF Pipeline mutates plan during Persistence
- QP Pipeline (runs after) sees hydrated IDs

## Cache Semantics

Cache loading moves from Config phase to Transaction phase.

### Current Flow

```
CF Config → cache.Load() → used in Transaction for ID hints
```

### New Flow

```
CF Transaction → cache.Load() → use alongside ApiFetch data
CF Persistence → cache.Update() + cache.Save() (unchanged)
```

This works because Transaction phase has ApiFetch data for validation. Cache provides ID hints for
previously-synced CFs.

## Implementation Progress

### Phase 1: Foundation (COMPLETED)

1. Created `PipelinePlan` class with hierarchical structure
2. Created `PlanDiagnostics` class for error/warning collection
3. Added `Plan` property to `PipelineContext` base class
4. Created plan model types: `PlannedCustomFormat`, `PlannedQualityProfile`, `PlannedCfScore`,
   `PlannedQualitySizes`, `PlannedMediaNaming`

### Phase 2: Plan Components (COMPLETED)

1. Created `IPlanComponent` interface for pluggable components
2. Extracted CF plan logic into `CustomFormatPlanComponent`
3. Extracted QP plan logic into `QualityProfilePlanComponent`
4. Extracted QS plan logic into `QualitySizePlanComponent`
5. Extracted MN plan logic into `MediaNamingPlanComponent`
6. Created `PlanBuilder` that orchestrates components via `IEnumerable<IPlanComponent>`
7. Registered components with `OrderByRegistration()` to preserve execution order

**Cleanup completed:**
- Made `IServiceBasedMediaNamingConfigPhase` sync (was async with `Task.FromResult`)
- Updated Radarr/Sonarr MN strategy implementations to return `MediaNamingDto` directly

### Phase 3: Pipeline Integration (COMPLETED)

1. ~~Modify `SyncProcessor` to call PlanBuilder first~~ DONE
2. ~~Add `DiagnosticsReporter` for consolidated logging~~ DONE
3. ~~Modify pipeline construction to receive plan~~ DONE
4. ~~Delete all ConfigPhase classes~~ DONE
5. ~~Delete CustomFormatLookup~~ DONE
6. ~~Delete ProcessedQualityProfileData~~ DONE
7. ~~Update Transaction phases to read from context.Plan~~ DONE
8. ~~Fix remaining test compilation errors~~ DONE

**Test updates completed:**
- Created `NewPlan.cs` with `Cf()`, `CfScore()`, `Qp()` test helpers
- Updated `NewQp.cs` to remove `Processed()` methods
- Deleted obsolete tests: `QualitySizeConfigPhaseTest.cs`, `QualityProfileConfigPhaseTest.cs`
- Updated QP tests to use `NewPlan.Qp()` and `context.Plan`
- Updated MediaNaming tests to use `context.Plan.MediaNaming`
- Updated `NamingFormatLookupTest` to use `InvalidNamingEntry`

**Key refactoring done:**
- `PlannedCfScore` now holds reference to `PlannedCustomFormat` (not just TrashId)
- Object graph naturally joins CF state - no hydration needed
- `PlannedQualityProfile.Config` replaces `.Profile` property
- All Transaction phases read from `context.Plan` instead of `context.ConfigOutput`
- DI registrations updated to remove ConfigPhase types

### Phase 4: CF/QS Pipeline Cleanup (COMPLETED)

1. ~~Update `PlannedQualitySizes` with new structure~~ DONE
2. ~~Update `QualitySizePlanComponent` to populate merged qualities~~ DONE
3. ~~Update `QualitySizeApiFetchPhase` to also fetch limits~~ DONE
4. ~~Update `QualitySizeTransactionPhase` to read from plan + resolve limits~~ DONE
5. ~~Update CF Transaction phase to read from `context.Plan.CustomFormats`~~ DONE
6. ~~Move cache loading to CF Fetch phase~~ DONE (not Transaction - Fetch needs it for RemoveStale)
7. ~~Remove `context.ConfigOutput` properties from QS context~~ DONE
8. ~~Fix test compilation errors~~ DONE
9. ~~Delete unused `QualityItemWithLimits` class and tests~~ DONE

### Phase 5: Testing (COMPLETED)

1. ~~Integration test: build complete plan from realistic config + guide resources~~ DONE
2. ~~Verify plan structure matches expected hierarchical output~~ DONE
3. ~~Test hydration flow (CF IDs populated after persistence)~~ DONE

**Test file:** `tests/Recyclarr.Cli.Tests/Pipelines/Plan/PlanBuilderIntegrationTest.cs`

Tests added:
- `Build_with_complete_config_produces_valid_plan` - CF happy path
- `Build_with_invalid_trash_ids_reports_diagnostics` - Error handling
- `Build_with_no_config_produces_empty_plan` - Edge case
- `Build_with_quality_definition_produces_quality_sizes_in_plan` - QS happy path
- `Build_with_invalid_quality_type_reports_error` - QS error handling
- `Cf_id_hydration_visible_to_qp_scores` - Hydration via object reference

## Files Created/Modified

### New Files (Plan Infrastructure)

```
src/Recyclarr.Cli/Pipelines/Plan/
├── IPlanComponent.cs              # Interface for pluggable components
├── PipelinePlan.cs                # Stateful plan with hydration support
├── PlanBuilder.cs                 # Orchestrates IEnumerable<IPlanComponent>
├── PlanDiagnostics.cs             # Error/warning collection
├── PlannedCustomFormat.cs         # CF plan model
├── PlannedQualityProfile.cs       # QP plan model + PlannedCfScore record
├── PlannedQualitySizes.cs         # QS plan model
├── PlannedMediaNaming.cs          # MN plan model
├── DiagnosticsReporter.cs         # Consolidated diagnostics logging
└── Components/
    ├── CustomFormatPlanComponent.cs
    ├── QualityProfilePlanComponent.cs
    ├── QualitySizePlanComponent.cs
    └── MediaNamingPlanComponent.cs
```

### Modified Files

- `PipelineContext.cs` - Added `Plan` property
- `PipelineAutofacModule.cs` - Added `RegisterPlan()` with ordered component registration
- `IServiceBasedMediaNamingConfigPhase.cs` - Changed to sync (returns `MediaNamingDto`)
- `RadarrMediaNamingConfigPhase.cs` - Removed `Task.FromResult` wrapper
- `SonarrMediaNamingConfigPhase.cs` - Removed `Task.FromResult` wrapper
- `MediaNamingConfigPhase.cs` - Updated to call sync strategy method
- `ISyncPipeline.cs` - Added `PipelinePlan` parameter to `Execute`
- `GenericSyncPipeline.cs` - Sets `context.Plan` from passed plan
- `CompositeSyncPipeline.cs` - Passes plan to child pipelines
- `SyncProcessor.cs` - Builds plan via `PlanBuilder`, reports diagnostics, passes plan to pipelines

## Migration Strategy

Direct replacement (no toggle/dual-path). Existing config phases are removed once plan components
are working.

**CRITICAL DIRECTIVE: Refactor mercilessly.** Do NOT keep old types, methods, or patterns around
"just in case." Delete unused code immediately. Keeping old stuff creates confusion and extra work.
When migrating to new patterns, delete the old code as soon as the new code works.

## Key Decisions Made

1. **PipelinePlan is a stateful class** - not a record/DTO. Supports mutation, has methods with
   business logic.

2. **Reference-based object graph** - `PlannedCfScore` holds a reference to `PlannedCustomFormat`,
   not just a TrashId. When CF persistence sets `Resource.Id`, QP scores automatically see the
   update via the shared reference. No hydration method needed - the object graph IS the join.

3. **Context-based propagation** - plan flows via PipelineContext.Plan, not DI injection. Explicit
   data flow.

4. **Diagnostics separate from plan** - enables pure plan components, consolidated error handling.

5. **All pipelines at once** - QS/MN are ~15-20% of work, simpler to do everything together.

6. **Direct replacement** - no backward compatibility toggle during migration.

7. **IPlanComponent interface** - all plan components implement sync `Process(plan, diagnostics)`
   interface. Components injected as `IEnumerable<IPlanComponent>` with `OrderByRegistration()` to
   preserve CF → QP ordering dependency.

8. **MN strategy made sync** - `IServiceBasedMediaNamingConfigPhase` changed from
   `Task<MediaNamingDto>` to `MediaNamingDto` since implementations only used `Task.FromResult`.

## External Validation

Nitsua (Notifiarr trash sync plugin developer) confirmed similar approach:

> "i build a list of profiles and their formats first, then do the format sync which will add
> anything missing from the pre-built profile list. if anything was added, it pulls the format id
> and modifies the profile list with its id so it doesnt fail"

## Related Documents

- `guide-sync-implementation-plan.md` - Overall phase plan (Phase 2 depends on this architecture)
- `cf-group-json-schema.md` - CF Group structure
- `quality-profile-json-schema.md` - Profile structure
- `resource-registry-architecture.md` - How guide resources are loaded
