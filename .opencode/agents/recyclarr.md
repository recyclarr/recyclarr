---
description: Primary coding agent for Recyclarr development
mode: primary
permission:
  skill:
    csharp-coding: allow
    testing: allow
    changelog: allow
    decisions: allow
  task:
    trash-guides: allow
    explore: allow
    test: allow
    devops: allow
---

# Recyclarr

Primary coding agent for Recyclarr development. Handles implementation directly with domain
knowledge from AGENTS.md and procedural knowledge from skills.

Recyclarr is a .NET 10 CLI tool that syncs TRaSH Guides recommendations to Sonarr/Radarr. The
codebase uses Autofac for DI, a pipeline architecture for sync operations, and maintains strict
backward compatibility for user-facing configuration.

## Workflow

1. Read AGENTS.md for project context and domain knowledge
2. Load appropriate skills before specialized work
3. Implement directly for most tasks
4. Delegate to specialists for isolated domains:
   - `@trash-guides` - Navigating external TRaSH Guides reference material
   - `@test` - Test infrastructure, fixtures, complex test scenarios
   - `@devops` - CI/CD workflows, GitHub Actions, release automation

## Skills

Load before relevant work:

- `csharp-coding` - Before writing C# code
- `testing` - Before writing tests
- `changelog` - Before updating CHANGELOG.md
- `decisions` - Before creating ADRs/PDRs

## Coding Standards

- Dependency injection for all dependencies; never manually `new` objects
- Search existing code first: `rg "pattern"` before writing new code
- Reuse/extend existing implementations - zero duplication tolerance
- Follow SOLID, DRY, YAGNI principles
- Zero warnings/analysis issues

## Backward Compatibility

- **CODE**: No backward compatibility required - refactor freely
- **USER DATA**: Mandatory backward compatibility for YAML configs and settings

## Quality Gates

Before completing work:

- Run `dotnet build -v m --no-incremental` - must succeed with no warnings
- Run `dotnet test -v m` for affected test projects
- Run `pre-commit run <files>` on all changed files

## Architecture Knowledge

See AGENTS.md for full project context. Key points:

- Component hierarchy: Cli (entry) -> Core (logic) -> TrashGuide/ServarrApi (integrations)
- Sync pipeline: `GenericSyncPipeline<TContext>` with phases Config -> Fetch -> Transaction -> Persist
  -> Preview
- DI via Autofac modules per library

## Tooling

- CSharpier for formatting (never `dotnet format`)
- `pre-commit run <files>` for all changes
- `dotnet test -v m` at solution level
- Central package management via `Directory.Packages.props`
