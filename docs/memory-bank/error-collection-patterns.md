# Unified Diagnostics System Design

## Reference Implementation

**POC Location**: `prototypes/DiagnosticsPreview/`

Run with:

```bash
dotnet run --project prototypes/DiagnosticsPreview -- [success|error|error-global]
```

The POC demonstrates the complete UX design including live progress display, diagnostic collection, and
final report rendering.

## Design Overview

### Sync Topology

1. **Load + Validate Config** - YAML parsing and FluentValidation
2. **Global Error Check** - If global errors exist, skip to diagnostic report
3. **Loop Instances** - Scoped DI per `IServiceConfiguration`
4. **Plan** - Build pipeline plans, collect warnings
5. **Pipelines** - Execute with diagnostic collection, skip failing pipelines
6. **Diagnostic Report** - Unified rendering at end of sync

### Event Scoping

Events are tagged with context via `SetInstance()` and `SetPipeline()`:

```txt
SyncEventCollector (lives entire sync)
├── Global: instance=null, pipeline=null (e.g., cross-cutting issues)
├── Instance: instance="sonarr-main", pipeline=null (e.g., instance-wide issues)
└── Pipeline: instance="sonarr-main", pipeline=CustomFormat (e.g., pipeline-specific errors)
```

- **Global/Instance**: Used for diagnostic report grouping
- **Pipeline**: Used for progress matrix (show `-` in correct column for errors)

### Diagnostic Types

- **Error** - Blocking; prevents pipeline execution
- **Warning** - Non-blocking; informational
- **Deprecation** - Non-blocking warning with `[DEPRECATED]` prefix and distinct styling

### SyncEventCollector API

See `ISyncEventCollector` interface. Key methods:

- `SetInstance(string?)` / `SetPipeline(PipelineType?)` - Set context (called by orchestrator)
- `AddError(string, Exception?)` - Logs error + collects DiagnosticEvent
- `AddWarning(string)` - Logs warning + collects DiagnosticEvent
- `AddDeprecation(string)` - Logs warning + collects DiagnosticEvent
- `AddCompletionCount(int)` - Collects CompletionEvent only (no log - pipeline loggers handle this)

All methods dual-write: collect for UI AND log via ILogger immediately.

## UX Design

### Live Progress Display

Table format with column headers (full names, two lines) and data matrix:

```txt
Legend: ✓ ok · ✗ failed · - error · -- skipped

                   Custom    Quality   Quality   Media
                   Formats   Profiles  Sizes     Naming
  ✓ sonarr-main    185       180       -         240
  ✓ radarr-4k      82        165       240       196
  ✓ radarr-anime   26        110       --        --
```

Styling:

- Active row (currently syncing): **bold**
- Completed/pending rows: normal weight
- Column headers: blue
- Success values: green
- Error indicator (`-`): red
- Skipped indicator (`--`): grey

Value meanings:

- `N` (number) - Synced N items
- `ok` - Ran successfully, no changes needed
- `-` - Error occurred, pipeline skipped
- `--` - Pipeline not configured/skipped

### Diagnostic Report

Rendered at end of sync in a panel:

```txt
╭─ Sync Diagnostics ─────────────────────────────────────────────────────────╮
│                                                                            │
│  Errors                                                                    │
│  ──────                                                                    │
│  • [sonarr-main] Invalid quality definition type "foo"                     │
│  • [sonarr-main] Quality "bar" not found in guide                          │
│                                                                            │
│  Warnings                                                                  │
│  ────────                                                                  │
│  • [global] [DEPRECATED] 'quality_profiles' syntax will be removed in v8.0 │
│  • [sonarr-main] Invalid trash_id "abc123"                                 │
│                                                                            │
╰────────────────────────────────────────────────────────────────────────────╯
```

Styling:

- Instance names: distinct colors (cyan, magenta, blue, green, yellow rotating)
- `[global]`: grey
- `[DEPRECATED]`: darkorange bold
- Error bullet: red
- Warning bullet: yellow

## Design Decisions

### Why Two Tiers, Not Three

Initially considered `SyncDiagnostics` → `InstanceDiagnostics` → `PipelineDiagnostics`. Simplified to
two tiers because:

- UX only displays instance-level grouping in the diagnostic report
- Pipeline status is shown in the progress matrix, not the diagnostic report
- Error messages naturally indicate which pipeline they relate to
- Per-pipeline blocking is a plan concern, not a diagnostic collection concern

### Real-Time vs Deferred Output

- **Real-time**: Progress display (spinners, counts as pipelines complete)
- **Deferred**: All errors, warnings, deprecations → collected during sync, rendered at end

This allows maximum flexibility in rendering and keeps the progress display clean.

### Pipeline Isolation

- Global error → skip entire sync, show diagnostics immediately
- Instance error → skip that instance, continue others
- Pipeline error → controlled by plan validation state (plan says "not available"), other pipelines
  in same instance continue

### Status Values in Progress Matrix

- `N` (number): Synced N items
- `ok`: Ran successfully, no changes needed
- `-`: Error occurred, pipeline skipped
- `--`: Pipeline not configured for this instance

## Unified Event Architecture

### Design Rationale

Pipelines need to report errors, warnings, and statistics. Multiple consumers need this data:

- **Notifications**: External push notifications via Apprise
- **Diagnostics**: Console panel at end of sync
- **Progress**: Console matrix showing per-pipeline status

Rather than separate collection systems, use unified event storage with multiple consumers.

### File Structure

```
src/Recyclarr.Core/Sync/
├── PipelineType.cs               # enum for pipeline identification

src/Recyclarr.Core/Sync/Events/
├── SyncEvent.cs                  # abstract record SyncEvent(InstanceName, Pipeline)
├── DiagnosticEvent.cs            # errors, warnings, deprecations
├── DiagnosticType.cs             # Error, Warning, Deprecation enum
├── CompletionEvent.cs            # pipeline completion counts
├── SyncEventStorage.cs           # List<SyncEvent>, singleton within sync
├── ISyncEventCollector.cs        # collection interface
└── SyncEventCollector.cs         # implementation with dual-write logging

src/Recyclarr.Cli/Pipelines/
├── PipelineContext.cs            # base class with abstract PipelineType property
├── GenericSyncPipeline.cs        # calls SetPipeline(context.PipelineType) before phases
└── {Pipeline}/                   # concrete contexts override PipelineType

src/Recyclarr.Cli/Processors/Sync/
├── SyncProcessor.cs              # calls SetInstance before each config
├── DiagnosticsRenderer.cs        # (future) reads storage → Spectre panel
└── ProgressRenderer.cs           # (future) reads storage → Spectre matrix

src/Recyclarr.Core/Notifications/
└── NotificationService.cs        # reads from SyncEventStorage → Apprise
```

### Event Types

See `src/Recyclarr.Core/Sync/Events/`:

- `SyncEvent` - Base record with `InstanceName` and `Pipeline` context
- `DiagnosticEvent` - Errors, warnings, deprecations (includes `DiagnosticType` enum)
- `CompletionEvent` - Pipeline completion counts

Instance and pipeline context are ambient - set once by orchestrator, applied to all subsequent events.

### Orchestration Flow

`SyncProcessor` sets instance context before each config. `GenericSyncPipeline` sets pipeline
context before executing phases. After all instances complete, notification is sent. Future:
DiagnosticsRenderer and ProgressRenderer will also consume events at end of sync.

### Consumer Pattern

All consumers read from same `SyncEventStorage`. For progress matrix cells: check for errors first
(show `-`), then check for completion count (show count, or `ok` if 0), otherwise show `--` for
unconfigured pipelines.

### Thread Safety

Not a concern. Instance processing and pipeline phases are sequential (`foreach` + `await`).
`List<SyncEvent>` is sufficient.

### Notification Verbosity

`VerbosityOptions` record (see `src/Recyclarr.Core/Notifications/VerbosityOptions.cs`) controls what
gets included in notifications. Errors and warnings are always sent. `SendInfo` controls completion
counts, `SendEmpty` controls whether to send when there's nothing to report.

## Scope Clarification

**SyncEventCollector is SYNC-SPECIFIC ONLY.** It lives entirely within `SyncProcessor` scope.

### Out of Scope (Keep Existing Patterns)

- **Provider init errors**: Exceptions → `ConsoleExceptionHandler` at `Program.Main`
- **Config validation**: `FilterContext` → `IFilterResultRenderer` (renders before instance loop)
- **CLI parsing, migrations**: Existing exception handling

### In Scope (SyncEventCollector)

- Plan phase errors (invalid trash_ids, missing profiles)
- Transaction phase warnings (conflicts, deprecations)
- Pipeline statistics (synced item counts)
- Pipeline-level issues that should be deferred to end-of-sync report

Config validation already has its own collection/rendering pattern (`FilterContext` + Spectre tree).
Unifying it with SyncEventCollector is a future consideration, not MVP.

## Implementation Plan

### Phase 1: PlanDiagnostics Migration (NEXT)

Migrate `PlanDiagnostics` to use `ISyncEventCollector` while **preserving immediate rendering timing**.

**Rationale**: Unify collection first so DiagnosticsRenderer has a single data source.

**Key Decision**: Plan errors continue to render immediately (before pipelines run), not deferred to
end-of-sync. Add a TODO comment in `SyncProcessor` to revisit timing once DiagnosticsRenderer is
complete.

**Files to modify**:

- `src/Recyclarr.Cli/Pipelines/Plan/PlanDiagnostics.cs` - Replace with `ISyncEventCollector` usage
- `src/Recyclarr.Cli/Pipelines/Plan/PlanBuilder.cs` - Update to use collector
- `src/Recyclarr.Cli/Pipelines/Plan/DiagnosticsReporter.cs` - Read from `SyncEventStorage`
- `src/Recyclarr.Cli/Processors/Sync/SyncProcessor.cs` - Add TODO comment about timing
- Plan components that inject `PlanDiagnostics`:
  - `CustomFormatPlanComponent` - invalid trash_ids
  - `QualityProfilePlanComponent` - duplicate CF score conflicts
  - `MediaNamingPlanComponent` - naming format validation
  - `QualitySizePlanComponent` - quality size errors

**Migration pattern**:

- `PlanDiagnostics.AddError()` → `eventCollector.AddError()`
- `PlanDiagnostics.AddWarning()` → `eventCollector.AddWarning()`
- `PlanDiagnostics.AddInvalidTrashId()` → `eventCollector.AddWarning()` with formatted message
- `PlanDiagnostics.AddInvalidNaming()` → `eventCollector.AddError()` with formatted message
- `PlanDiagnostics.ShouldProceed` → Query `SyncEventStorage` for errors

### Phase 2: DiagnosticsRenderer

- Implement `DiagnosticsRenderer` (Spectre panel matching POC design)
- Wire `SyncProcessor` to call renderer at end of sync
- Consider unifying plan + runtime diagnostics into single end-of-sync panel
- Delete old `DiagnosticsReporter` once timing is unified

### Phase 3: ProgressRenderer (Separate Work)

- Implement `ProgressRenderer` (Spectre matrix)
- Wire to display during/after sync
- Address preview mode questions
- Remove `log.Information()` calls for completion stats

## Current State (Session 2024-12-07)

### Event System Status

**Fully implemented**:

- `SyncEventCollector` + `ISyncEventCollector` - complete API
- `SyncEventStorage` - in-memory list storage
- Event types: `DiagnosticEvent`, `CompletionEvent`, `SyncEvent` base
- `DiagnosticType` enum: Error, Warning, Deprecation
- `PipelineType` enum: CustomFormat, QualityProfile, QualitySize, MediaNaming
- DI registration in `CoreAutofacModule.RegisterSyncEvents()`

**Integration points**:

- `SyncProcessor` calls `SetInstance()` before each config
- `GenericSyncPipeline` calls `SetPipeline()` before phases
- Runtime consumers: `CustomFormatTransactionLogger`, `QualityProfileLogger`, etc.
- `NotificationService` reads `SyncEventStorage` for external notifications

### Plan Phase Status

**Current (to be migrated)**:

- `PlanDiagnostics` - separate collection class
- `DiagnosticsReporter` - immediate Spectre panel rendering
- Plan components inject `PlanDiagnostics` directly

**Gap**: Plan errors don't flow through `SyncEventCollector`, creating two parallel systems

## Open Questions (Future Sessions)

### Preview Mode Impact

How does `--preview` mode affect the progress/diagnostics system?

- Preview skips persistence phase - what counts/status should the matrix show?
- Should diagnostics still be collected and rendered?
- Current preview phases render their own tables - does this conflict with progress matrix?

### Pipeline-Level Blocking

Currently `PlanDiagnostics.ShouldProceed` returns false if ANY error exists, blocking all pipelines.
New design: plan validation state per-feature. Questions:

- How does `PipelinePlan` expose per-feature validation state?
- Does `GenericSyncPipeline` check this before executing phases?
- Where does the "skip this pipeline" decision happen?

## Implementation Notes

### Plan Phase Errors

Plan phase CAN have errors (e.g., "quality definition type doesn't exist in guide"), but they should
be scoped per-pipeline. If QualitySize plan fails, skip only QualitySize pipeline, not all pipelines.

The plan object carries validation state per-feature. GenericSyncPipeline checks this before ApiFetch
and short-circuits if errors exist for that pipeline.

### Serilog vs Spectre.Console

- **Spectre.Console (IAnsiConsole)**: All user-facing output (progress, diagnostics)
- **Serilog (ILogger)**: File logging only, no console output

This separation prevents Serilog from interfering with Spectre's live rendering.
