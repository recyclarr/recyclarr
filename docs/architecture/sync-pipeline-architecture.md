# Sync Pipeline Architecture

## Design Philosophy and Context

Recyclarr's sync pipeline architecture addresses the fundamental challenge of synchronizing complex,
interdependent data between TRaSH Guides and Servarr applications. The design evolved from practical
experience with user pain points and the inherent complexity of media management synchronization.

### Why This Architecture Exists

**Complexity Reality**: Sync processing contains the majority of Recyclarr's business logic. The
complexity stems from validating user configuration, reconciling server state, handling dependencies
between data types (Quality Profiles reference Custom Formats), and managing service differences
between Sonarr and Radarr.

**User Experience Priority**: Most errors come from configuration mistakes and server-side
conflicts. The architecture prioritizes comprehensive error collection and user-friendly reporting
over fail-fast approaches, following a "spoon feeding" philosophy that explains problems in YAML
terms rather than technical internals.

**Evolution from Service-First**: The original service-first design (Radarr processing → Sonarr
processing) created significant code duplication since Custom Format processing is nearly identical
between services. The current category-first approach (Custom Formats → Quality Profiles → etc.)
eliminates this duplication while handling service differences through targeted injection points.

## Architecture Overview

The system processes four sync categories sequentially within each server instance:

1. **Custom Formats** (foundation) → 2. **Quality Profiles** (depends on CFs) → 3. **Quality
   Definitions** (independent) → 4. **Media Naming** (independent)

**User Experience**: Users see clear per-server processing markers, but pipeline internals remain
transparent. This choice prioritizes comprehension over architectural visibility.

**Dependency Management**: Sequential execution prevents reference errors (Quality Profiles need
existing Custom Format IDs) but sacrifices potential parallelization.

## Processing Model

Each sync category follows an identical pattern that separates concerns and enables comprehensive
error collection. Processing happens in two stages:

**Plan Phase** (pre-pipeline) → **Pipeline Phases**: ApiFetch → Transaction → Preview →
ApiPersistence

### Plan Phase (Pre-Pipeline)

The Plan phase validates configuration against TRaSH Guides data, catching invalid TrashIds and
resource conflicts before any server interaction. Plan components execute sequentially because some
depend on others (QP planning reads from the CF plan to build score assignments). The output is a
`PipelinePlan` consumed by subsequent phases.

### Pipeline Phases

**ApiFetch**: Server state is non-deterministic and changes independently. Fresh data is required
for accurate comparison and change planning.

**Transaction**: This is where complexity lives. Server-side validation requires runtime checks
against current state. Change planning must handle naming conflicts, dependency validation, and
update-vs-create decisions.

**Preview**: Users need to understand planned changes before committing, especially in production.
This implements dry-run by terminating the pipeline after displaying the transaction plan.

**ApiPersistence**: Execute changes with proper error handling and cache maintenance. All validation
is complete, so this focuses on execution reliability.

### Error Collection Strategy

The "collect and report later" pattern categorizes errors by source and timing:

- **Configuration Errors** (Plan phase): Invalid TrashIds, malformed YAML, resource provider
  conflicts
- **Server Validation Errors** (Transaction phase): Naming conflicts, missing dependencies, API
  constraint violations
- **Runtime Errors** (ApiPersistence phase): Network issues, authentication failures, service
  unavailability

**Context Objects**: Strongly-typed contexts carry all data and errors between phases, providing
complete audit trails without direct phase communication.

## Complexity and Challenges

### Real-World Complexity Example: Quality Profiles

Quality Profiles demonstrate why the pipeline architecture is necessary:

- **Multi-layered Dependencies**: Profiles reference Custom Formats that must exist first
- **Complex Scoring Logic**: Score assignment based on various TRaSH rule sets
- **State Reconciliation**: Merging user preferences with existing server configurations
- **Statistics Calculation**: Performance metrics and reporting requirements

This complexity would be unmanageable in a monolithic processor, making phase-based separation
essential.

### Service Abstraction Challenges

**Minor Differences, Major Impact**: Sonarr and Radarr are similar but have key differences in areas
like Media Naming formats and Quality Definition size limits. These differences require
service-specific implementations that sometimes result in over-engineering.

**Media Naming Over-Engineering**: Service differences in naming formats require complex abstraction
layers. While functionally necessary, this represents an area where the architecture may be more
complex than ideal.

### Architectural Evolution

**Iterative Refinement**: The pipeline system originally started more complex than necessary and has
been simplified over time. This evolution demonstrates the architecture's flexibility and capacity
for improvement.

**Ongoing Opportunities**: Current implementation likely still contains areas that could be
simplified, particularly around service-specific abstractions that could be more targeted.

## User Experience and Error Reporting

The architecture prioritizes user comprehension through a "spoon feeding" approach that translates
technical complexity into actionable guidance.

### Error Presentation Strategy

- **YAML-centric language**: Reference user-visible configuration rather than internal objects
- **Simple explanations**: Avoid technical jargon in favor of clear problem descriptions
- **Actionable guidance**: Provide specific steps and documentation links for resolution
- **Comprehensive collection**: Present all issues at once rather than fail-fast on first error

### Information Processing vs. UI Rendering

Clear separation between **what happened** (information processing) and **how to present it** (UI
rendering) enables:

- Comprehensive error collection without UI concerns
- Consistent presentation across output formats
- Testable diagnostic logic independent of presentation

This separation supports the debugging philosophy: every failure includes sufficient context for
understanding root causes without code inspection, with audit trails maintained through context
objects.

## Design Trade-offs and Future Considerations

### Conscious Compromises

**Reliability Over Performance**: Sequential processing simplifies error handling and debugging at
the cost of potential parallelization opportunities.

**Transparency vs. Simplicity**: Users have limited visibility into pipeline internals, prioritizing
comprehension over detailed progress reporting.

**Safety Over Speed**: Strongly-typed contexts and comprehensive validation add overhead but provide
auditability and error prevention.

### Extensibility Model

**Adding New Sync Categories**: Implement a plan component (`IPlanComponent`) and the four pipeline
phases with category-specific context.

**Handling Service Differences**: Use dependency injection for service-specific implementations
rather than duplicating entire processing paths.

**Phase Pattern Flexibility**: The current model works well for existing needs but may require
adaptation for future sync types with different processing requirements.

## Conclusion

The sync pipeline architecture represents a pragmatic solution that evolved from real-world
experience with the complexity of TRaSH Guides synchronization. The design successfully manages
substantial business logic through two-tier modularization (categories → phases) while prioritizing
user experience through comprehensive error reporting.

**Key Success Factors**:

- **Category-first structure** eliminates service duplication while handling differences through
  targeted injection
- **Plan + pipeline phases** separates concerns effectively and enables comprehensive validation
- **Context-driven communication** provides auditability and error accumulation without tight
  coupling
- **User-centric error reporting** translates technical complexity into actionable YAML-focused
  guidance

The architecture demonstrates resilience through its evolution from over-engineered origins to its
current refined state, suggesting the core patterns will continue serving future requirements as
Recyclarr's synchronization needs evolve.
