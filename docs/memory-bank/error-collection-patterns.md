# Error Collection Patterns Audit

## Problem Statement

Error/warning presentation to users is inconsistent across pipelines, creating maintenance burden and
inconsistent UX. Each pipeline has evolved its own pattern for collecting, storing, and presenting
errors.

## Current Patterns by Pipeline

### QualityProfile

- **Collection**: `QualityProfileTransactionData.NonExistentProfiles`, `InvalidProfiles`
- **Logging**: Dedicated `QualityProfileLogger` class
- **When**: Transaction phase populates, logger reports after transaction

### CustomFormat

- **Collection**: `CustomFormatTransactionData.ConflictingCustomFormats`
- **Logging**: Dedicated `CustomFormatTransactionLogger` class
- **When**: Transaction phase populates, logger reports in preview phase

### QualitySize

- **Collection**: None - logs inline
- **Logging**: Direct `ILogger.Warning()` in transaction phase
- **When**: Immediate during transaction execution
- **Gap**: No equivalent to QP's `NonExistentProfiles` for "quality doesn't exist on server"

### MediaNaming

- **Collection**: Plan-phase only via `NamingFormatLookup.Errors`
- **Logging**: Persistence phase only (`ILogger.Information()`)
- **When**: Plan phase validates, persistence reports success

### Plan Phase (PlanDiagnostics)

- **Collection**: `InvalidTrashIds`, `InvalidNamingFormats`, `Warnings`, `Errors`
- **Logging**: `DiagnosticsReporter` renders Spectre.Console panel
- **When**: After plan build, before pipeline execution
- **Issue**: `ShouldProceed` blocks ALL pipelines for feature-specific errors (violates isolation)

## Inconsistencies

1. **Storage Location**: Transaction data vs dedicated collections vs inline logging
2. **Logging Class**: Dedicated loggers (QP, CF) vs direct ILogger (QS) vs persistence-only (MN)
3. **Timing**: Transaction phase vs preview phase vs persistence phase
4. **Output Method**: ILogger warnings vs IAnsiConsole panels vs both
5. **Granularity**: Collections of typed errors vs string messages vs inline logs

## Desired State (TBD)

Goals for refactor:

- Consistent error collection pattern across all pipelines
- Pipeline-specific errors should NOT block other pipelines
- Unified user-facing error presentation (single approach)
- Clear separation: collection vs presentation

Possible approaches:

1. **Extend PlanDiagnostics** - Add pipeline-specific error collections, report all at once
2. **Transaction-level diagnostics** - Each transaction returns errors, unified reporting after
3. **Pipeline result object** - Each pipeline returns success/warnings/errors, aggregated at end

## Related

- `PlanDiagnostics.ShouldProceed` should be removed (blocks all pipelines for QS errors)
- Memory bank: `pipeline-plan-architecture.md` documents plan phase design
