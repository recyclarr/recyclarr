# ADR-022: The CLI logs only to the console

- **Scope:** `src/Recyclarr.Cli/Logging/`, `src/Recyclarr.Cli/Server/HttpTrafficLogHandler.cs`

## Context

As an HTTP client of the server, the CLI has few log events of its own; CLI log files mostly
duplicated server log lines that the server already writes. Bug reports still need the requests and
responses as the CLI saw them. API client tools such as kubectl and gh write no log files and print
diagnostics on request.

## Decision

The CLI MUST NOT write log files; it logs only to the console. The server keeps the durable record.

- Without `--log`, `ILogger` is silent and `IAnsiConsole` carries all user output. An errors-only
  console logger stays active until command setup, so early failures still show.
- With `--log [level]`, log events go to the console and `IAnsiConsole` output is hidden.
- Each server request logs its method, URL, status, and duration at Debug. Request and response
  bodies log at Verbose (`--log verbose`). Headers are never logged, because they carry the API key.
- `log_janitor` in `settings.yml` applies to server logs only.

## Rejected Options

- CLI log files with their own retention setting: duplicates the server's record.

## Consequences

- The CLI has no log directory or retention setting.
- A user captures a full exchange with `--log verbose` and a redirect.
- A failed CLI run leaves no log unless the user reruns it with `--log`.

## History

- 2026-09-27: Remove CLI log files; log HTTP traffic to the console under `--log`.
