# ADR-030: Registered pipeline graph for planning and execution

- **Scope:** `src/Recyclarr.Core/Pipelines/`: pipeline registration, planning, and orchestration

## Context

Planning ran plan components in Autofac registration order, and one shared plan object held a field
for every resource. Quality profile planning read the custom format plan only because custom formats
were registered first, so the dependency was invisible and unchecked. Sync operations already ran in
dependency order, but they declared dependencies separately and silently ignored dependencies on
unregistered pipelines. Adding a pipeline meant editing the shared plan and the registration order.

## Decision

Register each pipeline once with its identity, its dependencies, its planner, its plan type, and its
sync operation (`PipelineAutofacModule.RegisterPipeline`).

- One dependency list orders planning and execution. A dependency's plan is available to the
  dependent planner, and a dependency that does not succeed blocks the dependent operation.
- A planner reads another pipeline's plan only through `PlanningContext.Require`, and only from a
  declared dependency. Reading any other plan fails.
- A planning dependency exists when a planner refers to resources that another pipeline syncs, such
  as quality profile scores referring to custom formats. Guide data or config that no other pipeline
  syncs is injected directly, not read from another plan.
- Each pipeline's sync outcomes reach only its own operation. Planning outcomes belong to the
  instance (ADR-015), and an instance-blocking error from any pipeline fails the instance.
- A planner that returns no plan skips its pipeline. Its outcomes still count.
- `PipelineGraph` rejects duplicate pipelines, duplicate plan types, dependencies on unregistered
  pipelines, and cycles before any planner runs.

## Rejected Options

- Separate planning and execution dependency lists: every current edge appears in both, so a second
  list adds declarations and a consistency check with no current use. Split the list when the first
  pipeline needs to be blocked by a pipeline whose plan it does not read, or the reverse.
- Registration order for planning: hides dependencies, and a reordering breaks planning without a
  compile or startup error.
- One shared plan object with a field per resource: every new pipeline edits a central type, and any
  planner can read or write any other pipeline's plan.

## Consequences

- Adding a pipeline needs one registration, a planner, a plan type, and a sync operation; planning
  traversal, orchestration, and the plan container do not change.
- Ordering mistakes fail when the graph is built, not as wrong sync results.
- A plan type identifies its pipeline, so two pipelines cannot share one.

## History

- 2026-10-18: Pipelines register once; one dependency graph orders planning and execution.
