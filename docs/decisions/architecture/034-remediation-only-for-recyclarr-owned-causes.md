# ADR-034: Remediation only for Recyclarr-owned causes

- **Scope:** User-facing errors, warnings, and failure descriptions in every project (logs, CLI
  output, API reference text, notifications)

## Context

Many failures Recyclarr reports start outside it: a Sonarr or Radarr setting, a reverse proxy, the
network, filesystem permissions. Recyclarr can see that an external system refused a request, but
not how that system is configured or which settings its version accepts. Steps for configuring
another application go stale as that application changes, and they can be wrong on day one. For
example, advice to leave Sonarr's Allowed Hosts blank fails when Sonarr refuses a blank value under
some authentication settings.

## Decision

Messages MUST give step-by-step remediation only for things Recyclarr owns: configuration YAML,
settings files, CLI options and commands, environment variables Recyclarr reads, and its own data,
state, and log directories.

For an external cause, a message MUST name the cause and the external setting or component involved,
and MUST NOT explain how to configure that external system. Pointing the user at the external system
("check the reverse proxy", "rename the duplicates in the service") is allowed; describing that
system's values, formats, or procedures is not.

When a cause could be either, give the Recyclarr-owned remediation and name the external candidate.

## Rejected Options

- No remediation for external causes at all: the user loses the most likely place to look, such as a
  duplicate resource in the service that only the user can rename.
- Full instructions for external systems: Recyclarr cannot verify them against the user's version or
  configuration, so they become wrong without any change in Recyclarr.

## Consequences

- Messages for external causes are less prescriptive; the external application's documentation owns
  how to change its settings.
- The wiki may link to external documentation for common external causes.

## History

- 2026-11-13: Step-by-step remediation is limited to causes Recyclarr owns.
