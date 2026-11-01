# PDR-009: Starter configs generated from the guide

- **Upstream:** User feedback during the v9 pre-release, aggregated in the private maintainer note
  "Feedback: v9 pre-release (Discord, October 2026)"

## Context

Recyclarr 9 removed `config create` and `config list templates` when the CLI became an HTTP client
of the server. New users then had to copy starter files by hand from the
`recyclarr/config-templates` repository, and pre-release users asked why config creation was gone.

That repository is generated from TRaSH Guides quality profiles by a script, so its files lag the
guide and need a maintainer to regenerate them. Asked how useful `config create` is, users said they
run it at most once, when starting out, and some copy guide examples instead.

## Decision

Recyclarr generates starter configs at runtime from the TRaSH Guides data it already loads, one
template per guide quality profile. `config create` and `config list templates` return with their v8
syntax and output layout. The official config-templates repository is no longer downloaded;
`type: config-templates` providers remain for user-hosted include templates.

Scope stays at v8 parity. Wizards, choice-driven generation, and other extensions wait for user
requests.

## Rejected Options

- Keep the config-templates repository as the template source: its files drift from the guide
  between regenerations and cost a maintainer step per guide change.
- Document manual copying only: leaves new users without a working starting point, the gap
  pre-release users reported.
- Build a guided config wizard: the feedback shows the command is used once at most, which does not
  justify the cost.

## Consequences

- Config: None. Existing `type: config-templates` providers keep working.
- Migration: not required
- Template IDs are guide quality profile file names, so a renamed profile file changes its ID.
- Profiles in the guide's `TEST` profile group get no template.
- The config-templates repository can be archived after v9 ships.

## History

- 2026-10-31: Generate starter configs from guide data at v8 parity; stop downloading the official
  config-templates repository.
