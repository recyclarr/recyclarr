# ADR-022: The CLI logs only to the console

- **Status:** accepted
- **Date:** 2026-09-27

## Context and Problem Statement

The CLI wrote debug and verbose log files and pruned them with the log janitor. As an HTTP client of
the server, its own log events are few; most of its log files held forwarded server log lines, which
the server already writes to its own files. The CLI still needs a record of its exchange with the
server for bug reports, for example when it cannot use a response.

## Decision Drivers

- Diagnostic output must not duplicate what the server already records
- Bug reports need the requests and responses as the CLI saw them
- API client tools such as kubectl and gh write no log files; they print diagnostics on request

## Considered Options

1. Keep CLI log files with their own retention setting
2. Remove CLI log files and log HTTP traffic to the console under `--log`

## Decision Outcome

Chosen option: "Remove CLI log files", because the console is enough for an HTTP client and the
server keeps the durable record.

- Without `--log`, `ILogger` is silent and `IAnsiConsole` carries all user output. An errors-only
  console logger stays active until command setup, so early failures still show.
- With `--log [level]`, log events go to the console and `IAnsiConsole` output is hidden.
- Each server request logs its method, URL, status, and duration at Debug. Request and response
  bodies log at Verbose (`--log verbose`). Headers are never logged, because they carry the API key.
- `log_janitor` in `settings.yml` applies to server logs only.

### Consequences

- Good, because the CLI has no log directory or retention setting
- Good, because a user can capture a full exchange with `--log verbose` and a redirect
- Bad, because a failed CLI run leaves no log unless the user runs it again with `--log`
