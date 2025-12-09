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

### Interface Segregation

The event system uses interface segregation to enforce role-based dependency injection:

**ISyncScopeFactory** (context management):

- `SetInstance(string?)` - Set current instance context, returns `IDisposable` scope
- `SetPipeline(PipelineType?)` - Set current pipeline context, returns `IDisposable` scope
- Used by: Orchestrators (`SyncProcessor`, `GenericSyncPipeline`)

**ISyncEventPublisher** (event publishing):

- `AddError(string, Exception?)` - Logs error + collects DiagnosticEvent
- `AddWarning(string)` - Logs warning + collects DiagnosticEvent
- `AddDeprecation(string)` - Logs warning + collects DiagnosticEvent
- `AddCompletionCount(int)` - Collects CompletionEvent only
- Used by: Publishers (plan components, pipeline phases, loggers)

**SyncEventStorage** (reading/querying):

- `Diagnostics`, `Completions`, `AllEvents` - Access collected events
- `HasInstanceErrors(string instanceName)` - Query for instance errors
- Used by: Readers (`DiagnosticsRenderer`, `NotificationService`, `SyncProcessor`)

`SyncEventCollector` implements both `ISyncScopeFactory` and `ISyncEventPublisher`. All publishing
methods dual-write: collect for UI AND log via ILogger immediately.

**Design principle**: Nothing should be both a reader and a publisher. Inject only what your role
needs - this prevents leaky abstractions and makes dependencies explicit.

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
├── SyncEventStorage.cs           # storage + query methods (HasInstanceErrors)
├── ISyncScopeFactory.cs          # context management interface (SetInstance, SetPipeline)
├── ISyncEventPublisher.cs        # publishing interface (AddError, AddWarning, etc.)
└── SyncEventCollector.cs         # implements both interfaces with dual-write logging

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

### Phase 1: PlanDiagnostics Migration (DONE)

Migrated `PlanDiagnostics` to use unified event system while **preserving immediate rendering timing**.

**Completed**:

- Deleted `PlanDiagnostics.cs`
- `PlanBuilder` injects `ISyncEventPublisher`, returns only `PipelinePlan`
- `DiagnosticsRenderer` reads from `SyncEventStorage`, matches prototype UI exactly
- `SyncProcessor` queries `eventStorage.HasInstanceErrors(instanceName)` for plan errors
- Plan components use `ISyncEventPublisher` directly with inline message formatting
- Scoped context pattern: `SetInstance`/`SetPipeline` return `IDisposable` that auto-clears on dispose

### Phase 2: DiagnosticsRenderer (DONE)

- `DiagnosticsReporter` renders Spectre panel matching POC design
- Called at end of sync in `SyncProcessor.Process()` (after all instances complete)
- Renders all diagnostics (plan + runtime) in single consolidated panel
- `SyncEventStorage` provides typed access: `Diagnostics`, `Completions`, `AllEvents`

### Phase 3: ProgressRenderer (Separate Work)

- Implement `ProgressRenderer` (Spectre matrix)
- Wire to display during/after sync
- Address preview mode questions
- Remove `log.Information()` calls for completion stats

## Current State (Session 2024-12-09)

### Event System Status

**Fully implemented**:

- `SyncEventCollector` implements `ISyncScopeFactory` + `ISyncEventPublisher`
- `SyncEventStorage` - typed properties + `HasInstanceErrors(instanceName)` query method
- Event types: `DiagnosticEvent`, `CompletionEvent`, `SyncEvent` base
- `DiagnosticType` enum: Error, Warning, Deprecation
- `PipelineType` enum: CustomFormat, QualityProfile, QualitySize, MediaNaming
- DI registration in `CoreAutofacModule.RegisterSyncEvents()`

**Integration points by role**:

- **Orchestrators** (inject `ISyncScopeFactory`):
  - `SyncProcessor` - uses `syncScopeFactory.SetInstance()` for instance context
  - `GenericSyncPipeline` - uses `syncScopeFactory.SetPipeline()` for pipeline context

- **Publishers** (inject `ISyncEventPublisher`):
  - Plan components (`CustomFormatPlanComponent`, `QualityProfilePlanComponent`, etc.)
  - Pipeline phases (`MediaNamingApiPersistencePhase`, `QualitySizeTransactionPhase`, etc.)
  - Loggers (`CustomFormatTransactionLogger`, `QualityProfileLogger`)
  - Deprecation checks (`CfQualityProfilesDeprecationCheck`)

- **Readers** (inject `SyncEventStorage`):
  - `SyncProcessor` - uses `eventStorage.HasInstanceErrors(instanceName)` for error checks
  - `DiagnosticsRenderer` - reads `Diagnostics` for console panel
  - `NotificationService` - reads `AllEvents` for external notifications

### Diagnostics Rendering (COMPLETED)

- `DiagnosticsRenderer.Report()` called at END of sync (in `SyncProcessor.Process()`)
- Renders ALL diagnostics from ALL instances in single panel
- Matches prototype UI: instance colors, deprecation styling, error/warning sections
- Injected directly into `SyncProcessor` (not scoped per-config)

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
