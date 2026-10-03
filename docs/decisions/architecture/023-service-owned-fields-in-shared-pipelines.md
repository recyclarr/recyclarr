# ADR-023: Service-owned fields and behavior in one pipeline per resource

- **Status:** accepted
- **Date:** 2026-10-03
- **Supersedes:** the "Shared vs. service-specific pipeline boundary" section of ADR-005

## Context and Problem Statement

ADR-005 chose between two pipeline shapes by asking whether Sonarr and Radarr share a meaningful
domain concept. Every resource shares some fields, so the answer depends on how much they share, and
the two shapes solve the same problem differently:

- Quality profiles use shared types end to end. Radarr's profile `Language` is a nullable property
  on those types, and only the Radarr gateway writes it. Nothing above the gateway knows the field
  is Radarr-only, so Sonarr desired state carries the guide language and every Sonarr sync reports a
  language change it can never apply.
- Media naming registers a separate sync operation per service under one pipeline identity. Both run
  through orchestration, and `ShouldSkip` drops the one whose plan is empty. Orchestration has to
  filter skipped operations before ordering them only because two operations share one key.

## Decision Drivers

- One rule must apply to every pipeline, with no judgment about how much two services share
- A field that one service does not have must be impossible to express for that service
- Service selection should use one mechanism throughout the sync engine
- Shared logic should not be duplicated per service

## Considered Options

1. Keep ADR-005's boundary and fix quality profile `Language` locally
2. Split every pipeline with any service-only field into per-service pipelines
3. Place each field by ownership, and keep one operation per pipeline with per-service components

## Decision Outcome

Chosen option: "Place each field by ownership", because it gives every pipeline the same structure
and lets the type system enforce which service a field belongs to.

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

- Each pipeline has exactly one sync operation and one plan component. Orchestration never sees more
  than one operation for a pipeline identity.
- The operation owns the flow shared by both services. Behavior that depends on service-owned fields
  lives in per-service components behind one interface, resolved by the instance's service through
  DI, as gateways (`RegisterServiceGateway`) and Quality Size limit fetchers already are.
- Shared logic that service components need lives in helpers both components depend on, not in a
  base class.

### Superseded rules

- ADR-005: "Same resource with service-specific properties" no longer maps service-only fields to
  optional properties on a shared domain type.
- ADR-005: "Different resource concepts behind a shared path" no longer produces service-specific
  pipelines. Such a resource is one pipeline whose types are mostly service-specific.

ADR-005's port and gateway layer, gateway state management, and one gateway per service are
unchanged.

### Consequences

- Good, because every pipeline follows one structure regardless of how much the services share
- Good, because a field written for the wrong service is a compile error rather than a recurring
  phantom change
- Good, because orchestration no longer needs to handle two operations under one pipeline identity
- Bad, because a pipeline with few shared fields gains a component interface that mostly forwards to
  service-specific code
- Bad, because adding a service-only field touches every layer it passes through instead of one
  nullable property
