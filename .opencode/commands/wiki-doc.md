---
description: Draft documentation follow-up for a completed change, as a Linear issue or inline prompt
---

Produce documentation follow-up material for a change already implemented in this session (or
described in `$ARGUMENTS`). Output either a Linear tracking issue or a self-contained prompt for a
separate wiki-updating AI session, depending on the mode selected below.

## Mode selection

Parse `$ARGUMENTS` as natural language to determine intent:

- Phrases like "issue", "ticket", "linear", "file it", "for later", "track it" -> **issue mode**
- Phrases like "prompt", "inline", "now", "right now", "paste", "in another session" -> **prompt
  mode**
- Ambiguous content (just framing like "focus on the new YAML field"): treat as framing, not mode;
  ask the user which mode they want.
- Empty `$ARGUMENTS`: ask the user which mode they want.

When asking, use the question tool with two options: "Linear issue (for later)" and "Inline prompt
(update the wiki now)". Do not proceed without an answer.

Any non-mode content in `$ARGUMENTS` is extra framing to apply during synthesis (e.g. "focus on the
new YAML field").

## Process (shared by both modes)

### 1. Gather context

Build a complete picture of the user-visible change from session context and `$ARGUMENTS`:

- What features, fixes, or behavior changes occurred
- The intent and rationale behind each change
- User impact: new capabilities, changed behavior, removed functionality, new or changed YAML config
  properties, CLI flag or command changes, changed defaults, changed error messages
- Concrete YAML or CLI examples with real values (not placeholder tokens) when configuration changed

If `$ARGUMENTS` references specific artifacts, investigate them comprehensively:

- Commit SHAs or ranges: read with `git log` / `git show` and extract the meaningful changes
- GitHub issues or PRs: fetch their content, comments, and linked items
- Linear issues (`REC-` prefix): read them including comments
- Follow cross-references between issues, PRs, and commits to build complete understanding
- Read any source code necessary to understand behavioral changes

### 2. Identify the driving issue

Find the GitHub or Linear issue that originally drove this work. Search in this order and stop at
the first hit:

1. Explicit references in `$ARGUMENTS` or recent session messages (`REC-NNN`, `#NNN`)
2. Current branch name: `!git rev-parse --abbrev-ref HEAD`
3. Recent commits on the branch: `!git log -20 --oneline` (look for `REC-` or `#NNN` in subjects)
4. `CHANGELOG.md` `[Unreleased]` section for related entries

Include the reference (issue ID and one-line title) in the output. If nothing turns up, state that
explicitly: "Driving issue: not identified." Never fabricate an ID.

### 3. Classify backward compatibility

Every change MUST be classified explicitly. A change is "breaking" only if previously documented and
supported behavior now requires the user to modify their YAML config, CLI scripts, or workflow to
avoid errors. Those belong in the upgrade guide.

Not breaking: fixing undefined or undocumented behavior, tightening validation on previously invalid
input, or changing undocumented implementation details. Even if someone happened to depend on the
accidental behavior, it was never part of Recyclarr's contract. These do NOT belong in the upgrade
guide.

For each change, state breaking or not, and why. If there are no breaking changes, say so
explicitly: "No breaking changes. No upgrade guide entries needed." Do not leave this to the
reader's interpretation.

### 4. Self-contained content rule

Embed all factual context directly in the output. Do not reference commits, PRs, or issues by
identifier alone; always include the substantive information from those sources inline. The reader
(doc-writing agent or human picking up the ticket later) should not need to chase IDs to understand
WHAT and WHY.

Focus on WHAT changed and WHY. Do NOT instruct HOW the documentation should be written, what tone to
use, which wiki pages to edit, or how to structure headings. The doc writer owns those decisions.

## Mode: prompt

Output a single self-contained prompt as the ONLY output. No preamble, no postamble, no code blocks
wrapping it, no quotation marks around it. The receiving agent has no access to this repository, git
history, or any issue tracker, so everything it needs must be in the prompt text.

The prompt must include:

- Summary of what changed
- Rationale for each change
- User impact bullets
- YAML or CLI examples with real values when applicable
- A dedicated "Backward Compatibility" section with explicit per-change classification
- The driving issue reference (or explicit "not identified")

## Mode: issue

Load the `linear-cli` skill before running any `linear` CLI commands.

### Dedupe check

Before creating anything, search Linear for an open Documentation-labeled issue covering the same
change. Match on the user-visible change, not exact wording. If a likely duplicate exists, ABORT:
print the existing issue URL and title and stop. Do not create a new issue.

### Compose

**Title**: `Docs: <concise description of what changed>` (under ~80 chars). Examples: `Docs: new
include_unmonitored option for quality profile sync`, `Docs: --dry-run now skips cache writes`.

**Body** (markdown, in this order):

```markdown
## Summary
One paragraph: what changed, in user terms.

## Why
The rationale. What problem it solves or what gap it fills.

## User impact
- Bullet list of every user-visible effect
- New/changed/removed YAML keys with their location in the config
- New/changed/removed CLI flags or commands
- Changed defaults, limits, or error messages

## Configuration example
(Only if YAML or CLI usage changed. Use real values, not placeholder tokens.)

## Backward compatibility
Explicit per-change classification: breaking or not breaking, with one sentence of justification.
If none are breaking, state "No breaking changes. No upgrade guide entries needed."

## Driving issue
REC-NNN or #NNN with one-line title, or "not identified".

## Notes for the doc writer
Search hints for locating the change in the recyclarr repo (file paths, class names, config keys,
relevant PR subject lines). These are hints only; commit SHAs and line numbers go stale on rebase
and should not be treated as authoritative. Search the repo for the current state.
```

### Create

Use the `linear-cli` skill to create the issue with:

- Team: Recyclarr
- Project: none
- Labels: `Documentation`
- Status: `Todo`
- Title and body from above

Print the resulting issue URL as the final line of output. No summary of what you created; the URL
is enough.

## Rules

- WHAT and WHY only. Never prescribe HOW the docs should be written.
- Never fabricate issue IDs, commit SHAs, PR numbers, YAML keys, or CLI flags. If unknown, say so.
- Issue mode: abort on duplicate; do not wait for user approval before creating; do not summarize
  what you created beyond the URL.
- Prompt mode: the prompt is the entire output; no wrapping, no commentary.
- Commit SHAs are safe to read during synthesis but unsafe to cite as authoritative references in a
  Linear issue body (the user rebases frequently). Hints only.
