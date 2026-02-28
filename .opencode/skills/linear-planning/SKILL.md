---
name: linear-planning
description: >
  Use when creating, organizing, or managing Linear issues, projects, or initiatives for Recyclarr
  work planning
---

# Linear Planning

Planning and work tracking conventions for the Recyclarr Linear workspace.

## Hybrid Approach

Linear tracks **work items and progress**. The git repo holds **design reference and architectural
knowledge**. Never duplicate between the two; each has one job.

- **Linear**: Issues, projects, status, dependencies, progress
- **Repo**: ADRs, architecture docs, design rationale (`docs/architecture/`, `docs/decisions/`)

Linear project descriptions SHOULD link to relevant repo docs (ADRs, architecture docs). Repo docs
MUST NOT track work status; that is Linear's job.

## Linear Hierarchy

Use Linear's hierarchy to match the natural scope of work:

| Concept    | Scope                                     | Example                           |
|------------|-------------------------------------------|-----------------------------------|
| Initiative | Strategic goal spanning multiple projects | (not currently used)              |
| Project    | Large deliverable with clear outcome      | Gateway Layer, Refit Migration    |
| Issue      | One independently testable chunk of work  | Phase 5: Custom Formats gateway   |
| Sub-issue  | Breakdown of a single issue               | Only when a phase needs splitting |

### Decision Rules

- **Too large for one issue, has a clear outcome?** Make it a project.
- **Too large for one issue, too small for a project?** Use parent + sub-issues.
- **Independently testable and deliverable?** Single issue.
- **Need to split work across people or steps within an issue?** Sub-issues.

### Projects

Projects represent large units of work with a clear outcome. They group related issues and provide
progress tracking.

- Assign a project lead
- Link to relevant architecture docs and ADRs in the project description
- Use `blockedBy` issue relations for cross-project dependencies (e.g., a Phase 7 issue in Project B
  blocked by a Phase 6 issue in Project A)

### Issues

Each issue represents one complete, independently testable and deliverable chunk of work.

- Use `blockedBy` relations to encode dependencies between issues
- Labels distinguish categories (e.g., `Part A`, `Part B`) when issues share a project
- Sub-issues are only needed when a single issue's scope requires further breakdown during
  implementation

## What NOT to Put in Linear

- Design patterns and architectural rationale (belongs in `docs/architecture/`)
- Decision records (belongs in `docs/decisions/`)
- Living design reference that AI agents read during coding (belongs in repo)

## Workspace Conventions

- Issue prefix: `REC-` (e.g., REC-74)
- Statuses: Backlog, Todo, In Progress, Done, Canceled, Duplicate
- Transition to "In Progress" when starting work
- Transition to "Done" after the final commit lands
- Always check issue comments when reading issue details
