# ADR-021: The CLI reads its own optional settings file

- **Scope:** `src/Recyclarr.Cli` settings loading, `schemas/cli-schema.json`

## Context

`settings.yml` belongs to the server, which validates it strictly. In centralized mode the CLI and
the server often run on different machines, so a shared file would force each process to accept or
ignore the other's keys. Most users run the ephemeral server and should need no extra file, and
`settings.yml` is released user data that keeps its name.

## Decision

The CLI reads its own optional file, `cli.yml`; the server keeps `settings.yml`. MUST NOT add CLI
keys to `settings.yml` or server keys to `cli.yml`.

- `cli.yml` lives in the configuration directory (ADR-020). If it is absent, every default applies
  and commands use an ephemeral server.
- `server.base_url` selects centralized mode (ADR-010); `server.api_key` holds the server API key
  (ADR-027).
- A non-empty `RECYCLARR_SERVER_URL` overrides `server.base_url`, so a container image can target
  its own server without a file in the user's volume. An empty value falls back to the file.
- Parsing is strict. An unknown key or a base URL that is not absolute http or https stops the
  command with a message that names the file.
- The CLI reads the file only when a command connects to a server.
- `schemas/cli-schema.json` describes the file.

## Rejected Options

- One shared `settings.yml` with a section per process: one schema describes keys two processes
  own.
- Rename both files (`settings.server.yml`, `settings.cli.yml`): breaks released server settings.

## Consequences

- Each process validates only its own keys.
- A user who runs both on one machine edits two files.

## History

- 2026-09-27: The CLI reads `cli.yml`; `settings.yml` belongs to the server.
- 2026-10-06: A non-empty `RECYCLARR_SERVER_URL` overrides `server.base_url`.
- 2026-10-14: Add `server.api_key`.
