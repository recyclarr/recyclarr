# Logging

Only the HTTP server writes log files (ADR-022). The CLI logs to the console, and only with `--log`.

| Process | Output | Contents |
| --- | --- | --- |
| CLI | console, with `--log` | Command lifecycle, HTTP traffic, and forwarded server events |
| Server | `<data>/logs/server` | Hosting, requests, sync execution, and internal failures |

Each server run writes a `debug` log (Debug and above) and a `verbose` log (Verbose events only) to
its directory. The CLI logs each server request at Debug and request and response bodies at
Verbose; it never logs headers.

`log_janitor.max_files` counts server log files. The server cleans its logs during startup and never
deletes the files of the current run.

## User-visible diagnostics

Core returns structured outcomes, operational failures, and opaque fault references in terminal
sync results. The Server formats that data for its log and HTTP responses. CLI renderers remain
presentation-only and consume response DTOs.

User-actionable diagnostics cross the HTTP boundary as structured response data. Each process logs
the diagnostics at its own boundary:

- Normal mode displays `IAnsiConsole` output; the CLI logger is silent.
- `--log` suppresses `IAnsiConsole`; the CLI logger writes to stdout.

This behavior is the same for an ephemeral child server and a configured remote server. Internal
exception details are not part of the API contract and remain in the server log.

The process that understands a deprecation detects it. The server logs configuration problems and
deprecations once, at startup; they are not part of any HTTP response (ADR-019). CLI-specific
deprecations remain client-side.

In ephemeral mode, the server also forwards log events over the child process stdout protocol.
This provides live server detail in `--log` mode. The server log remains the authoritative record
for the server process.
