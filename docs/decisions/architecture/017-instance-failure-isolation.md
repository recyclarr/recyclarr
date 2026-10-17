# ADR-017: Isolate unexpected faults by service instance

- **Scope:** `src/Recyclarr.Core/Sync/SyncOrchestrator.cs`, `InstanceSyncProcessor`

## Context

Service instances are independent. Stopping the run at the first unexpected exception would block
unrelated instances and attribute the fault to the run instead of the instance where it occurred.
Consumers need that attribution without exception details, and cancellation must still stop the
run.

## Decision

An unexpected exception in one instance becomes an opaque fault on that instance, and the run
continues with the next instance.

An instance attempt includes creating its lifetime scope, processing the instance, and disposing the
scope. A catchable exception within that boundary produces one terminal instance result with an
opaque fault reference. Available planning outcomes and pipeline results are retained, and the next
configured instance is attempted.

The fault contributes to instance and run status aggregation. It is not an outcome and does not
expose exception text, stack traces, request bodies, or service responses. Expected service failures
remain typed operational failures. An instance can retain both an expected failure and a cleanup
fault when both occurred.

Cancellation signaled through the run token propagates and stops the run. An
`OperationCanceledException` without that signal is an unexpected instance fault. Failures outside
an instance attempt remain run-level faults. Fault reporting is best effort and cannot erase a
terminal result or stop later instances.

## Rejected Options

- Stop the run at the first unexpected exception: one instance's bug blocks all others.
- Convert unexpected exceptions into expected operational failures: hides software faults behind
  service failure categories.

## Consequences

- Completed work and fault attribution reach every result consumer.
- Consumers handle faults at both run and instance level.
- Cleanup faults and primary failures need deterministic preservation rules.

## History

- 2026-09-17: Isolate unexpected faults per instance and continue the run.
- 2026-09-24: Cancellation counts only when the run token is signaled.
