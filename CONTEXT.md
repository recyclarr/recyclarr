# Recyclarr sync

Recyclarr sync reconciles configured intent with the state of a Sonarr or Radarr instance.

## Language

**Sync job**:
The lifecycle created after a sync request and its server-owned configuration pass validation.

**Sync run**:
The execution of accepted service instances within a sync job.

**Sync request**:
A request to create a sync job. Invalid request selectors are client errors; invalid server-owned
configuration is a server fault.

**Sync result**:
A terminal account of a sync run, instance, or pipeline. It contains a status, outcomes, and
deltas.

**Plan**:
The result of analyzing configuration and cached guide data before any service interaction.

**Transaction**:
The read-only comparison of a plan with current service state. It produces outcomes and deltas.

**Persistence**:
The application of calculated deltas to service and sync state.

**Status**:
A summary classification of how a sync result concluded.

**Succeeded**:
Every intended resource completed its required stage. A no-op success is succeeded.

**Partial**:
At least one intended resource completed and at least one did not.

**Failed**:
No intended resource completed, or a pipeline-wide failure prevented a usable result.

**Blocked**:
The pipeline did not run because a dependency was not succeeded.

**No-op success**:
A pipeline completed with no deltas because it had no resources to sync or service state already
matched the plan. It satisfies downstream dependencies.

**Outcome**:
An expected semantic condition that explains a sync decision, rejection, skip, or failure.
_Avoid_: Diagnostic, message

**Delta**:
A computed semantic difference between observed and desired service state. A delta does not assert
that Recyclarr applied the change or persisted it atomically. A resource delta may contain smaller
value or structure deltas.
_Avoid_: Message

**Value delta**:
The current and desired values of one semantic value within a resource delta.

**Fault**:
An unexpected operational or software failure during a sync. A fault is not an outcome.

**Progress**:
A transient observation of ongoing sync execution. Progress is not part of a terminal sync result.

**Preview**:
A sync mode that returns planning and transaction results without persistence.

**Custom Format adoption**:
The association of an unmanaged, same-named service Custom Format with a managed Trash ID.

**Selection provenance**:
The configuration or guide relationship that caused Recyclarr to select a Custom Format.

**Reference mismatch**:
A configured or guide-backed reference that cannot be resolved against current service state. It
does not assign fault to either side.

**Guide-backed Quality Profile**:
A Quality Profile identified by both a Trash ID and a configured name.

**User-defined Quality Profile**:
A Quality Profile identified only by its configured name.
