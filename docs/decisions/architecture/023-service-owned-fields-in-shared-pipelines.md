# ADR-023: Service-owned fields and behavior in one pipeline per resource

- **Scope:** every sync pipeline under `src/Recyclarr.Core/Pipelines/` and the types it passes
  through

## Context

Sonarr and Radarr resources always share some fields, so a rule based on how much they share needs
judgment and produced two shapes. With shared types end to end, Radarr's quality profile `Language`
was a nullable property that only the Radarr gateway wrote, so every Sonarr sync reported a language
change it could never apply. With one operation per service under one pipeline identity (media
naming), orchestration had to filter skipped operations before ordering them.

## Decision

Place each field by service ownership, and give every pipeline one sync operation with per-service
components.

### Models

- A field that both services have belongs on a shared type. A field that only one service has
  belongs on that service's type. This applies at every layer the field passes through: guide
  resource, plan, domain data, desired state, deltas, results, and response DTOs.
- How many fields are shared changes only how much code is shared, never which structure applies. A
  resource with almost no shared fields, such as media naming, has mostly service-specific types; a
  resource with almost no service-only fields, such as quality profiles, has mostly shared types.
- Nullability does not express service ownership. A nullable property on a shared type means "may
  have no value", not "this service does not have the field".

### Behavior

- Each pipeline has exactly one sync operation and one planner. Orchestration never sees more than
  one operation for a pipeline identity.
- The operation owns the flow shared by both services. Behavior that depends on service-owned fields
  lives in per-service components behind one interface, resolved by the instance's API target
  through DI (`RegisterServiceGateway`, ADR-028).
- Shared logic that service components need lives in helpers both components depend on, not in a
  base class.

## Rejected Options

- Nullable shared properties for service-only fields, fixed case by case: the type system cannot
  stop a field reaching the wrong service.
- Per-service pipelines for any resource with a service-only field: duplicates the shared flow and
  puts two operations under related identities.

## Consequences

- Every pipeline has one structure regardless of how much the services share.
- A field written for the wrong service is a compile error, not a recurring phantom change.
- A pipeline with few shared fields gains a component interface that mostly forwards.
- A service-only field touches every layer it passes through.

## History

- 2026-10-03: Place fields by service ownership; one operation per pipeline.
