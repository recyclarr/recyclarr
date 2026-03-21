---
name: changelog
description: Use when updating CHANGELOG.md for a release or user-facing change
---

# Changelog Skill

CHANGELOG format and conventions for documenting Recyclarr releases.

Recyclarr uses keepachangelog.com format. The audience is non-technical end users who run Recyclarr
but do not read source code. They care about what changed in their experience, not why or how.

## CHANGELOG Format

File: `CHANGELOG.md`

Section order: Added, Changed, Deprecated, Removed, Fixed, Security

Entry format: `- Scope: Description (#NNN)`

```markdown
### Fixed

- Sync: Crash while processing quality profiles (#720)
```

## Rules

- One line per change
- Entries under "Fixed" should not start with "Fixed"
- New entries go under `[Unreleased]` section near the top of the file
- Keep entries high-level: state what was added or changed, not an exhaustive list of capabilities.
  Users can visit the docs for details.
- Include a GitHub issue reference `(#NNN)` at the end of the entry when an issue exists. Omit when
  there is no associated issue.
- Sort entries within each category by user impact: breaking changes first, then high-impact items,
  then minor items. Do not use commit chronology as the ordering.
- Every Deprecated entry must include what to use instead or a migration path. A deprecation without
  guidance is incomplete.
- Omit entries that cancel each other out within the same unreleased window. If a feature was added
  and then reverted before release, remove both entries.

## Implementation Detail Filter

CRITICAL: Changelog entries must contain ZERO implementation details. Every entry must pass this
filter before being written.

**Never mention any of the following:**

- Library or package names (Spectre.Console, YamlDotNet, Autofac, NUnit, etc.)
- Class, method, or variable names from source code
- Internal architecture concepts (pipeline, middleware, DI container, module, etc.)
- Protocol-level details (HTTP status codes, response bodies, serialization)
- Programming language concepts (null reference, exception type, stack trace, etc.)
- File paths or namespaces from the source tree
- Database, cache, or state file internals

**Litmus test:** If a reader would need to look at source code to understand a term, that term does
not belong in the changelog.

**Transformation process:** When writing an entry, draft it, then strip every technical term.
Restate the user-visible symptom or behavior in plain language.

### Examples

```markdown
# BAD: Leaks library name and internal concept
- CLI: Automatically switch to log output mode when stdout is redirected (non-TTY),
  preventing garbled output from Spectre.Console animations in cron jobs and piped
  commands

# GOOD: Describes the user-visible behavior only
- CLI: Automatically switch to log output mode when stdout is redirected, preventing
  garbled output in cron jobs and piped commands

# BAD: Exposes internal error type
- Config: NullReferenceException when quality_profiles section is empty

# GOOD: States the symptom
- Config: Crash when quality_profiles section is empty

# BAD: References internal architecture
- Sync: Pipeline transaction phase fails to persist custom format mappings to cache

# GOOD: States what the user observes
- Sync: Custom format changes not saved between syncs

# BAD: Mentions HTTP details
- Config: Trailing slash in base_url caused HTTP 401 Unauthorized from Sonarr API

# GOOD: States the user-facing result
- Config: Trailing slash in `base_url` caused authentication errors
```

## Breaking Changes Format

Required for any release with breaking changes:

```markdown
## [X.0.0] - YYYY-MM-DD

This release contains **BREAKING CHANGES**. See the [vX.0 Upgrade Guide][breakingX] for required
changes you may need to make.

[breakingX]: https://recyclarr.dev/guide/upgrade-guide/vX.0/

### Changed

- **BREAKING**: Description of breaking change
```

## Removed Features Wording

Check if deprecation was in a prior release:

```bash
git log --oneline --diff-filter=A -- "path/to/deprecation" | tail -1
git tag --contains <sha> | grep -E '^v[0-9]'
```

- **Tags exist**: "The deprecated `X` has been removed."
- **No tags**: "The `X` option has been removed. Use `Y` instead."
