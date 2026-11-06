# ADR-033: Outcome severity follows sync effect

- **Scope:** `src/Recyclarr.Core/Pipelines/`, `src/Recyclarr.Core/Sync/`, sync outcome codes in
  `src/Recyclarr.Server/Sync/Results/`

## Context

Each outcome needs a severity that the API, the CLI, and notifications all show the same way. When
each renderer chose severities on its own, they drifted apart: the same outcome showed as an error
in one place and a warning or info in another. Some errors also left pipeline status at `succeeded`,
so automation saw a clean run even when part of the configuration had no effect. Severity has to
describe what the sync did, so that status, exit code, and display agree.

## Decision

An outcome's severity MUST follow from its effect on the sync, decided once per outcome code:

- **Blocking error:** continuing would put wrong data on resources the outcome does not name, so the
  instance stops. ADR-015 owns the criterion for blocking planning outcomes.
- **Error:** a configured item did not sync, or a configured reference resolves to nothing. The rest
  of the instance stays correct.
- **Warning:** the configuration asks for something that cannot take effect, and the sync result is
  otherwise what the user asked for.
- **Info:** the result is what the user asked for; the outcome reports something notable or
  redundant.

A reference that resolves to nothing is an error even when Recyclarr could continue around it. A
mistyped name silently changes the result (for example, an unprotected score gets reset), and users
act on errors, not warnings.

Status MUST agree with severity:

- An error makes the item it names incomplete, so its pipeline ends `partial` or `failed`.
- A warning or info outcome never changes status.
- A blocking error makes the instance fail before any pipeline runs.

Severity belongs to the outcome code, not to a renderer. Renderers MUST NOT reassign it.

## Rejected Options

- Severity chosen by each renderer: renderers drift, and the API cannot promise one meaning.
- Errors that leave the item synced: status then reports success for a result the user did not ask
  for.
- Blocking for every error: one bad item would stop unrelated items that would sync correctly.

## Consequences

- Each new outcome code needs a severity decision against these criteria, and its producer must mark
  the item incomplete when the severity is error.
- A test can check that every error outcome affects status and no warning or info outcome does.
- Some configurations that synced with warnings now end `partial`.

## History

- 2026-11-05: Severity derives from sync effect; errors make their item incomplete.
