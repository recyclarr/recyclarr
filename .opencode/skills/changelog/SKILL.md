---
name: changelog
description: >-
  Use when adding, editing, or reorganizing entries in `CHANGELOG.md`; drafting
  release notes for Recyclarr; deciding which Keep a Changelog section (Added,
  Changed, Deprecated, Removed, Fixed, Security) a change belongs under;
  wording user-facing entries for `feat`, `fix`, `perf`, or breaking commits;
  phrasing deprecation notices or migration guidance; preparing the
  `[Unreleased]` section for cut. Triggers on phrases like "update the
  changelog", "add a changelog entry", "bump the changelog", "release notes",
  or any edit touching `CHANGELOG.md`. Do NOT use for commit message wording
  (see commit agent) or for internal `refactor`/`chore`/`test`/`ci`/`build`
  commits that do not require a changelog entry.
---

# Changelog Skill

CHANGELOG format and conventions for documenting Recyclarr releases.

Recyclarr uses keepachangelog.com format. The audience is non-technical end users who run Recyclarr
but do not read source code. They care about what changed in their experience, not why or how.

## Release Delta

Entries describe the difference between the last released version and this release, as seen by a
user who never ran an unreleased build. Commits are evidence for finding candidates, not entries.

- A feature new in this release gets one entry for the whole feature. Its properties (auth, ports,
  file formats, options) are not separate Added or Changed entries.
- Changed, Deprecated, and Removed apply only to behavior a released version had. Fixed applies only
  to bugs a released version had.
- Drop changes that cancel out or only refine unreleased work, such as a feature added and reverted,
  or a fix to an unreleased feature.
- Before writing an Added or Fixed entry, confirm the last release tag lacks it: search older
  `CHANGELOG.md` sections and `git grep <term> <tag>`. Unreleased commits can touch a released
  feature without introducing it.
- Test each entry: would a user upgrading from the last release recognize the "before" it implies?
  If not, the entry describes development history; remove or fold it into its feature entry.

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

When a release links an upgrade guide, its breaking and removed entries name only what changed.
Migration steps, replacement settings, and rationale belong in the guide, not the entry.

## Removed Features Wording

Check if deprecation was in a prior release:

```bash
git log --oneline --diff-filter=A -- "path/to/deprecation" | tail -1
git tag --contains <sha> | grep -E '^v[0-9]'
```

- **Tags exist**: "The deprecated `X` has been removed."
- **No tags**: "The `X` option has been removed. Use `Y` instead." (omit the replacement when an
  upgrade guide covers it)
