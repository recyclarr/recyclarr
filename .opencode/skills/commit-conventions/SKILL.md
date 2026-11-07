---
name: commit-conventions
description: >-
  Use when choosing a Recyclarr commit type or scope (feat, fix, perf, refactor, chore, test, ci,
  build, docs), deciding whether a change needs a CHANGELOG entry, or committing in this repository.
---

# Commit Conventions

Use this priority order (highest to lowest) to determine commit type.

## Tier 1: User-Facing (require CHANGELOG)

Ask: "Could a user report this as broken?" This includes crashes, garbled output, wrong results,
missing data, or any behavior that fails to meet reasonable expectations. If yes: `fix`. Otherwise,
ask: "Does this change user-visible behavior (e.g. new capability, enhanced output, modified
defaults)?" If yes: `feat`. If neither: skip to Tier 2.

- `fix:` -> Corrects behavior a user could report as broken (PATCH bump)
- `feat:` -> User-visible change that is not a bug fix (MINOR bump)
- `perf:` -> Significant, measurable performance improvement (PATCH bump)
- Append `!` for breaking changes regardless of type (MAJOR bump)

Multi-commit features: use `refactor` for infrastructure commits, `feat` only for the commit that
enables the user-visible capability. The `changelog` skill picks the CHANGELOG section.

## Tier 2: Path-Based (no CHANGELOG)

If Tier 1 doesn't apply, match file paths:

- `test:` -> `tests/**`
- `ci:` -> `.github/**`, `ci/**`
- `build:` -> `*.props`, `*.csproj`, `*.slnx`, `.config/dotnet-tools.json`
- `docs:` -> `docs/**`, `*.md` (including `CHANGELOG.md` if committed alone)

## Tier 3: Catch-All (no CHANGELOG)

If neither Tier 1 nor Tier 2 applies:

- `refactor:` -> C# changes in `src/**` (internal restructuring, code reorganization)
- `chore:` -> All other non-C# changes (`scripts/**`, `.renovate/*`, `renovate.json5`,
  `.editorconfig`, `.vscode/*`, `schemas/*`, linter configs, etc.)

## Scopes

A scope names a durable boundary a user or maintainer would filter the log by, never a type and
never a directory. Use one of these or omit the scope; commitlint (`scope-enum`) rejects others:

- `deps` -> dependency updates (Renovate)
- `sync` -> `src/Recyclarr.Core/{Pipelines,Sync}/*`, sync orchestration
- `config` -> `src/Recyclarr.Core/Config/*`
- `yaml` -> `schemas/*`
- `cli` -> `src/Recyclarr.Cli/*`
- `servarr` -> `src/Recyclarr.Api.{Sonarr,Radarr}/*`, `src/Recyclarr.Core/ServarrApi/*`
- `server` -> `src/Recyclarr.Server/*`
- `client` -> `src/Recyclarr.Client/*`
- `docker` -> Dockerfile and image assets (NOT Docker-related workflows; those are `ci:`)
- `agents` -> `AGENTS.md`, `.opencode/*`, skills, agent definitions

MUST NOT restate the type as a scope: `ci(ci)`, `docs(readme)`, `test(e2e)`, `docs(changelog)` and
similar add nothing. Scopeless `chore:` beats a one-off scope.

A new scope is justified only when all three hold: 3+ commits fit nothing existing, the name
survives a refactor (`docker` does, `di` and `http` do not), and it partitions rather than nests (no
`sync` plus `sync-qp`). Add it to `commitlint.config.mjs` and this list together. Retire a scope by
the same test once its boundary dissolves.
