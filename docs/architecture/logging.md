# Logging

The CLI and HTTP server own separate logs because they may run on different machines and have
different lifetimes.

| Process | Directory | Contents |
| --- | --- | --- |
| CLI | `<data>/logs/cli` | Command lifecycle, HTTP client failures, and API diagnostics |
| Server | `<data>/logs/server` | Hosting, requests, sync execution, and internal failures |

Each run writes a `debug` log (Debug and above) and a `verbose` log (Verbose events only, such as
HTTP request and response bodies) to its directory.

`log_janitor.max_files` counts files and applies independently to each directory. The CLI cleans
its logs when a command finishes. The server cleans its logs during startup and never deletes the
files of the current run.

## User-visible diagnostics

Core returns structured outcomes, operational failures, and opaque fault references in terminal
sync results. The Server formats that data for its log and HTTP responses. CLI renderers remain
presentation-only and consume response DTOs.

User-actionable diagnostics cross the HTTP boundary as structured response data. Each process logs
the diagnostics at its own boundary:

- Normal mode displays `IAnsiConsole` output and writes the CLI log.
- `--log` suppresses `IAnsiConsole`; the CLI logger writes to both the CLI log and stdout.

This behavior is the same for an ephemeral child server and a configured remote server. Internal
exception details are not part of the API contract and remain in the server log.

The process that understands a deprecation detects it. Server-side configuration deprecations are
returned for the CLI to present and record. CLI-specific deprecations remain client-side.

In ephemeral mode, the server also forwards log events over the child process stdout protocol.
This provides live server detail in `--log` mode. The server log remains the authoritative record
for the server process; the CLI log is the record for one command invocation.
