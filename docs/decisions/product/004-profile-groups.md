# PDR-004: Profile Groups

- **Status:** Accepted
- **Date:** 2025-12-13
- **Upstream:** [PR #2561](https://github.com/TRaSH-Guides/Guides/pull/2561)

## Context

TRaSH Guides quality profiles span multiple categories (Standard, Anime, French, German, SQP) but
there was no machine-readable way to organize them into logical groups for third-party sync apps.

## Decision

Add "profile groups" concept to TRaSH Guides organizing quality profiles into logical categories.
This was implemented and merged in PR #2561.

## Affected Areas

- Config: None (future enhancement opportunity)
- Commands: None
- Migration: Not required

## Consequences

- Enables third-party sync apps to present organized profile selection
- Future Recyclarr enhancement: could use profile groups for UI/config organization
