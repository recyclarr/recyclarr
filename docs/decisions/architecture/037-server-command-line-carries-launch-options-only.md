# ADR-037: Server command line carries launch options only

- **Scope:** `src/Recyclarr.Server/Program.cs`, `src/Recyclarr.Server/Hosting/` command-line parsing
  and host configuration

## Context

ASP.NET Core reads command-line arguments into the same configuration as environment variables and
accepts any `--key=value`. Every value the server reads from its environment could therefore also be
passed as an argument: a config directory, or an API key visible to every local user in the process
list. Its parser silently skips some malformed arguments and throws on others.

## Decision

The server MUST parse its command line with System.CommandLine into launch options and MUST NOT pass
raw arguments to host configuration.

- A launch option is a value a launcher chooses for one process and that no other Recyclarr process
  needs, for example the listen URLs or the CLI's ownership of a server it started (ADR-010).
  Everything else comes from environment variables and `settings.yml` (ADR-036, ADR-027).
- Judge every accepted argument by its effect, whether our code or System.CommandLine handles it. An
  argument that changes how the server runs MUST be a launch option. An argument that only prints
  information and exits without starting the server is allowed, for example `--help`.
- An unknown argument stops startup with exit code 1: the parse error goes to stderr and usage help
  to stdout.
- Arguments come only from the command line; response files (`@file`) are disabled.
- Options only the CLI passes are hidden from help.
- System.CommandLine does not handle process termination. The host handles SIGTERM and Ctrl+C so
  that shutdown stays graceful under systemd, Windows services, and containers.
- Build-time OpenAPI document generation runs the entry point with its own host arguments and
  bypasses parsing.

## Rejected Options

- Raw arguments in host configuration: any variable, secrets included, becomes an argument.
- A hand-written allow-list: reimplements tokenizing, value parsing, errors, and help that the
  official parser provides.
- Switch mappings on the configuration provider: they rename keys but do not reject unknown ones.

## Consequences

- A new launch option is a System.CommandLine option mapped onto `ServerLaunchOptions`.
- `recyclarr-server --help` and `--version` work.
- Generic host arguments such as `--environment` are not accepted; the matching environment variable
  (`ASPNETCORE_ENVIRONMENT`) sets them.

## History

- 2026-11-28: Parse the command line into launch options; reject unknown arguments.
