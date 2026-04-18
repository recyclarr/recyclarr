---
name: decisions
description: >-
  Use when creating, editing, superseding, or reviewing Architecture Decision
  Records (ADRs) or Product Decision Records (PDRs) under `docs/decisions/`
  (including `docs/decisions/architecture/` and `docs/decisions/product/`);
  writing MADR-formatted decision documents; recording rationale for an
  architectural or upstream-driven choice; linking a decision to a TRaSH
  Guides upstream source; changing a decision's status (proposed, accepted,
  deprecated, superseded). Triggers on phrases like "write an ADR", "new
  PDR", "document this decision", "decision record", "add an architecture
  decision", or any edit to files matching `docs/decisions/**/*.md`. Do NOT
  use for general architecture documentation under `docs/architecture/`.
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

**Reference-style links**: Use reference-style links for long URLs to respect line length limits:

```markdown
- **Upstream:** [Discord DM][upstream] with TRaSH Guides contributor

[upstream]: https://discordapp.com/channels/@me/...
```

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

PDRs MUST have `upstream:` linking to the external driver. Private links (DMs, gated channels) are
acceptable; the link provides provenance even if not publicly accessible. If no upstream link is
available, ask the user for one before finalizing the PDR.

## Attribution

**No individual names in PDR body.** Use roles ("TRaSH Guides contributor", "upstream maintainer")
rather than personal names. Reasons:

- Roles outlast individuals
- Reduces friction in candid discussions
- Avoids taking statements out of context

**Upstream link is the audit trail.** The link provides who/when/full-context; the PDR body provides
what/why synthesis. If someone questions a decision and cannot access the link, the PDR rationale
SHOULD stand on its own merits.

**Private links are valid.** Discord DMs, private channels, or gated sources are acceptable upstream
references. The link serves as:

- Provenance for those with access
- Good faith signal that a source exists
- Future-proofing if access later expands

## Perspective

TRaSH Guides is the authoritative upstream; Recyclarr is a downstream consumer. PDRs document
Recyclarr's response to guide decisions. Focus on:

- Recyclarr implementation impact
- User experience improvements
- TRaSH Guides maintainer/contributor benefits (upstream health benefits all consumers)

Exclude references to other sync tools - they're peers responding to the same upstream, not relevant
to Recyclarr's decisions.
