# Documentation Directory Guide

## Directories

- `architecture/` - Current system design (what is, not alternatives)
- `decisions/` - MADR-based decision records
  - `architecture/` - ADRs: technical implementation (how)
  - `product/` - PDRs: strategic/upstream-driven (what/why)
- `reference/` - External materials (Discord summaries, upstream docs)
- `memory-bank/` - AI working memory

## Decision Records

**MANDATORY**: Read the relevant `TEMPLATE.md` before creating or editing ANY decision document.

- ADR template: `decisions/architecture/TEMPLATE.md`
- PDR template: `decisions/product/TEMPLATE.md`

Naming: `NNN-kebab-case-title.md` (sequential per category)

### When to Create

**ADR** - Implementation choices, patterns, internal tradeoffs:

- Choosing between implementation approaches
- Establishing patterns affecting multiple components
- Making tradeoffs with long-term maintenance implications

**PDR** - Upstream changes, feature scope, external drivers:

- Responding to upstream schema or API changes
- Deciding feature scope or priorities
- Tracking external decisions that drive Recyclarr development

PDRs require `upstream:` frontmatter linking to the external driver (issue, PR, Discord thread).
