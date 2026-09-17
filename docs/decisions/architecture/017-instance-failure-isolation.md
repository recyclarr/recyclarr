# ADR-017: Isolate unexpected faults by service instance

- **Status:** accepted
- **Date:** 2026-09-17

## Context and Problem Statement

Service instances are independent, but the first unexpected exception currently stops the whole sync
run. This prevents later instances from running even when the exception is specific to one instance,
and it assigns the fault to the run instead of the instance where it occurred.

## Decision Drivers

- One instance must not prevent unrelated instances from syncing
- Completed work and planning context must survive an unexpected exception
- Unexpected exceptions must remain distinct from expected semantic outcomes and service failures
- Consumers need instance attribution without receiving exception details
- Cancellation and failures outside an instance attempt still need to stop the run

## Considered Options

1. Stop the run after the first unexpected instance exception
2. Convert unexpected exceptions into expected operational failures
3. Retain an opaque fault on the affected instance and continue

## Decision Outcome

Chosen option: "Retain an opaque fault on the affected instance and continue", because instances are
independent and an unexpected exception does not establish that later instances will also fail.

An instance attempt includes creating its lifetime scope, processing the instance, and disposing the
scope. A catchable exception within that boundary produces one terminal instance result with an
opaque fault reference. Available planning outcomes and pipeline results are retained, and the next
configured instance is attempted.

The fault contributes to instance and run status aggregation. It is not an outcome and does not
expose exception text, stack traces, request bodies, or service responses. Expected service failures
remain typed operational failures. An instance can retain both an expected failure and a cleanup
fault when both occurred.

Cancellation propagates and stops the run. Failures outside an instance attempt remain run-level
faults. Fault reporting is best effort and cannot erase a terminal result or stop later instances.

### Consequences

- Good, because independent instances continue after one instance encounters a software fault
- Good, because completed work and fault attribution remain available to every result consumer
- Good, because unexpected failures remain separate from expected domain outcomes
- Bad, because result consumers must handle faults at both run and instance boundaries
- Bad, because scope cleanup and primary processing failures need deterministic preservation rules
