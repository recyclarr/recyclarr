---
name: decisions
description: Creating and editing decision records (ADRs for architecture, PDRs for product/upstream)
---

# Decision Records

Procedural knowledge for creating ADRs (Architecture Decision Records) and PDRs (Product Decision
Records) in `docs/decisions/`.

## Templates

**MANDATORY**: Read the relevant template before creating or editing ANY decision document.

- ADR template: `docs/decisions/architecture/TEMPLATE.md`
- PDR template: `docs/decisions/product/TEMPLATE.md`

## Rules

**One decision per record.** Do not bundle multiple decisions into a single ADR/PDR. Each decision
should be independently trackable, referenceable, and supersedable.

**Naming**: `NNN-kebab-case-title.md` (sequential per category)

**Date accuracy**: Use the date the decision was made, not when documented. For existing decisions,
verify against git history (`git log --format="%ai" <commit> -1`).

## Status Values

- `proposed` - Under consideration, not yet decided
- `accepted` - Decision made and finalized
- `deprecated` - No longer applies (context changed)
- `superseded by {ADR,PDR}-NNN` - Replaced by another decision

For accepted decisions with pending implementation details, use `accepted` and note the pending
aspects in the document body.

## When to Create

**ADR** - Implementation choices, patterns, internal tradeoffs:

- Choosing between implementation approaches
- Establishing patterns affecting multiple components
- Making tradeoffs with long-term maintenance implications

**PDR** - Upstream changes, feature scope, external drivers:

- Responding to upstream schema or API changes
- Deciding feature scope or priorities
- Tracking external decisions that drive Recyclarr development

PDRs require `upstream:` frontmatter linking to the external driver (issue, PR, Discord thread).

## Perspective

TRaSH Guides is the authoritative upstream; Recyclarr is a downstream consumer. PDRs document
Recyclarr's response to guide decisions. Focus on:

- Recyclarr implementation impact
- User experience improvements
- TRaSH Guides maintainer/contributor benefits (upstream health benefits all consumers)

Exclude references to other sync tools - they're peers responding to the same upstream, not relevant
to Recyclarr's decisions.
