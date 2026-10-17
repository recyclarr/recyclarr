---
name: decisions
description: >-
  Use when creating, editing, or reviewing Architecture or Product Decision Records (ADRs, PDRs):
  recording why an architectural or upstream-driven choice was made, linking it to a TRaSH Guides
  source, or changing, replacing, or removing one. Triggers on "write an ADR", "new PDR",
  "decision record", or any edit to `docs/decisions/**/*.md`.
---

# Decision Records

Procedural knowledge for creating ADRs (Architecture Decision Records) and PDRs (Product Decision
Records) in `docs/decisions/`.

## Templates

**MANDATORY**: Read the relevant template before creating or editing ANY decision document.

- ADR template: `docs/decisions/architecture/TEMPLATE.md`
- PDR template: `docs/decisions/product/TEMPLATE.md`

## Rules

Records carry no status. Every record on `master` is in force; Git history is the full record of
past versions.

**One decision per record.** Do not bundle multiple decisions into a single ADR/PDR. State each rule
once, in the record that owns it, and link to it from other records instead of restating it.

**Naming**: `NNN-kebab-case-title.md`, sequential per category. MUST NOT reuse a number, including
numbers of deleted records; check `git log --diff-filter=D --name-only -- docs/decisions/` when the
highest number may have been deleted.

**Body is the current decision.** Write every section as if the decision was always this way. MUST
NOT add amendment prose ("previously", "updated to", "originally"), superseded options presented as
history, or "Status" lines. Former choices that a reader could propose again belong in Rejected
Options.

**History**: one line per decision change, oldest first: `- YYYY-MM-DD: {what changed}`. The first
line records the original decision. Edits that do not change the decision (typos, links,
formatting, wording) get no line.

**Date accuracy**: Use the date the decision was made or changed, not when documented. For existing
decisions, verify against git history (`git log --format="%ai" <commit> -1`).

## Changing a Decision

- Decision modified: rewrite the affected sections in place and append a History line. Rename the
  file and title when the old title no longer describes the decision; keep the number.
- Decision replaced by a different approach: rewrite the same record to the new decision.
- Decision dropped with no replacement: delete the record and update every reference to it
  (`rg -n 'ADR-NNN|NNN-' docs src tests .opencode AGENTS.md`).

**Reference-style links**: Use reference-style links for long URLs to respect line length limits:

```markdown
- **Upstream:** [Discord DM][upstream] with TRaSH Guides contributor

[upstream]: https://discordapp.com/channels/@me/...
```

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
