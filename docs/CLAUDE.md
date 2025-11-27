# Documentation Directory Guide

## Directory Structure

### architecture/

Authoritative documentation of current system design. These documents describe **what is**, not what
was considered or rejected.

- Describes current implementation reality
- Updated when architecture changes
- No historical alternatives or rejected ideas

### decisions/

Architecture Decision Records (ADRs) capturing significant design choices and their rationale. These
documents explain **why** we chose a particular approach, including alternatives considered.

- Preserves decision context for future maintainers
- Documents rejected alternatives to prevent re-litigation
- Numbered sequentially (001, 002, etc.)

### reference/

External reference materials, research notes, and supporting documentation not authored for this
project but useful for context.

## ADR Template

ADRs must follow this structure:

```markdown
# ADR-NNN: [Short Decision Title]

## Status

[Accepted | Superseded by ADR-NNN | Deprecated]

## Context

[What is the issue? What forces are at play? 2-4 sentences describing the problem space.]

## Decision

[What is the change being proposed? State the decision clearly and concisely.]

## Rationale

[Why this decision? Bullet points explaining the reasoning.]

## Alternatives Considered

[What other options were evaluated? Why were they rejected? Use subsections for each
alternative with brief explanation.]

## Consequences

[What are the implications? Both positive tradeoffs accepted and negative impacts to be aware
of.]
```

### ADR Naming Convention

Files: `NNN-kebab-case-title.md` (e.g., `001-resource-query-service-dispatch.md`)

### ADR Numbering

Use the next sequential number. Check existing files in `decisions/` before creating.
