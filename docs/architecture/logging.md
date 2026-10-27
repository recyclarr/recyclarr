# Logging

Only the HTTP server writes log files (ADR-022). The CLI logs to the console, and only with `--log`.

| Process           | Output                              | Contents                                                 |
| ----------------- | ----------------------------------- | -------------------------------------------------------- |
| CLI               | console, with `--log`               | Command lifecycle and HTTP traffic                       |
| Persistent server | stdout and files; OTLP when enabled | Hosting, requests, sync execution, and internal failures |
| Ephemeral server  | errors on stderr, and files         | Same as the persistent server                            |

`server.logging` in `settings.yml` controls the server's outputs:

- `format`: the persistent server's stdout is human-readable text on a terminal and JSON lines when
  stdout is redirected (containers, systemd, services), where a collector reads it; `json` or `text`
  overrides the detection. JSON keys follow OpenTelemetry names for logs that are not OTLP:
  `timestamp` (UTC), `level`, `message`, `message_template`, `trace_id`, `span_id`,
  `exception.type`, `exception.message` (sanitized), `exception.stacktrace`, then event properties.
- `files`: on by default in every mode and platform, so every installation keeps a log; `false`
  turns them off where stdout is collected instead (for example, Kubernetes).

Each run that writes files writes a `debug` log (Debug and above) and a `verbose` log (Verbose
events only) to `<data>/logs/server`. The CLI logs each server request at Debug and request and
response bodies at Verbose; it never logs headers.

`log_janitor.max_files` counts server log files. The server cleans its logs during startup and never
deletes the files of the current run.

Until startup has validated `settings.yml`, the server writes only errors to stderr, because the
configured outputs are not known to be valid yet. A startup failure before that point (invalid
settings, or a management port that equals the API port) is reported there, with the YAML line for
settings problems.

## Telemetry

When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, the server exports traces, metrics, and logs over OTLP,
configured only by the standard `OTEL_*` variables. OTLP logs follow the console level. The
persistent server also serves Prometheus metrics on its management port (ADR-029) unless
`server.management.metrics` is false. Health probes and scrapes produce no spans, HTTP server
metrics, or request log entries.

Each sync job run is its own trace. A job created by an API request links to that request's span;
scheduled jobs have no link. The job runs in a clean execution context, so state inherited by the
TickerQ worker (the logging scope and activity of the request that created it) never reaches the
job's logs or spans.

## User-visible diagnostics

Core returns structured outcomes, operational failures, and opaque fault references in terminal sync
results. The Server formats that data for its log and HTTP responses. After a sync job ends, the CLI
fetches the terminal results and formats the response DTOs for the console and its log. CLI
renderers remain presentation-only.

User-actionable diagnostics cross the HTTP boundary as structured response data. Each process logs
the diagnostics at its own boundary:

- Normal mode displays `IAnsiConsole` output; the CLI logger is silent.
- `--log` suppresses `IAnsiConsole`; the CLI logger writes to stdout.

This behavior is the same for an ephemeral child server and a configured remote server. Internal
exception details are not part of the API contract and remain in the server log.

The process that understands a deprecation detects it. The server logs configuration problems and
deprecations once, at startup; they are not part of any HTTP response (ADR-019). CLI-specific
deprecations remain client-side.

The CLI does not forward server log events. In ephemeral mode, the server writes only error events
to stderr so the CLI can explain a server that fails to start (ADR-010); the server log remains the
authoritative record for the server process.
