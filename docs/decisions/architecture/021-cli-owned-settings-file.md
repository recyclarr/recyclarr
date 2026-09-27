# ADR-021: The CLI reads its own optional settings file

- **Status:** accepted
- **Date:** 2026-09-27

## Context and Problem Statement

The CLI read `server.base_url` from `settings.yml`, which the server owns and validates strictly. In
centralized mode the CLI and the server often run on different machines. A shared file forces each
process to accept or ignore the other's keys, and one schema then describes keys that two
processes own.

## Decision Drivers

- Each settings file should have one owner that validates it strictly
- On one machine, the CLI and the server should use the same configuration directory
- Most users run the ephemeral server and should need no extra file
- `settings.yml` is released user data and keeps its name

## Considered Options

1. One shared `settings.yml` with a separate section for each process
2. Separate files, `settings.yml` for the server and `cli.yml` for the CLI
3. Rename both files to `settings.server.yml` and `settings.cli.yml`

## Decision Outcome

Chosen option: "Separate files", because each file has one owner and existing server settings
files keep working.

- `cli.yml` is optional and lives in the configuration directory (ADR-020). If it is absent, every
  default applies and commands use an ephemeral server.
- `server.base_url` selects centralized mode (ADR-010). REC-153 adds the API key beside it.
- Parsing is strict. An unknown key or a base URL that is not absolute http or https stops the
  command with a message that names the file.
- The CLI reads the file only when a command connects to a server.
- `schemas/cli-schema.json` describes the file.

### Consequences

- Good, because each process validates only its own keys
- Good, because ephemeral users see no change
- Bad, because a user who runs both on one machine edits two files
