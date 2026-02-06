---
name: changelog
description: Use when updating CHANGELOG.md for a release or user-facing change
---

# Changelog Skill

CHANGELOG format and conventions for documenting Recyclarr releases.

Recyclarr uses keepachangelog.com format. The audience is non-technical end users.
Load this skill when updating CHANGELOG.md.

## CHANGELOG Format

File: `CHANGELOG.md`

Section order: Added, Changed, Deprecated, Removed, Fixed, Security

Entry format: `- Scope: Description`

```markdown
### Fixed

- Sync: Crash while processing quality profiles
```

## Rules

- Audience is non-technical end users
- One line per change
- Entries under "Fixed" should not start with "Fixed"
- New entries go under `[Unreleased]` section near the top of the file

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
