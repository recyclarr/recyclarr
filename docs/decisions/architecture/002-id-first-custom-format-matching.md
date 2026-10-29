# ADR-002: ID-first custom format matching with automatic adoption

- **Scope:** `src/Recyclarr.Core/Pipelines/CustomFormat/CustomFormatSyncOperation.cs`, sync state

## Context

Custom format sync must match each guide CF to a CF in Sonarr or Radarr. Name-first matching broke
when Sonarr allowed case-variant names ("HULU", "Hulu") to coexist: Recyclarr picked an arbitrary
match and the API rejected the write as "Must be unique" (issue #672). Sync state already records
the service ID that each trash_id owns, and IDs do not change when a CF is renamed.

## Decision

Match by the service ID stored in sync state first; use names only to find a CF that state does
not know. Name comparison is case-insensitive.

1. Stored ID exists in the service: update that CF, whatever its name.
2. Stored ID is gone, or no state entry exists: look up the name.
   - No match: create the CF.
   - Exactly one match: adopt it (record its ID in state) and report an adoption outcome.
   - Two or more matches: report an ambiguous-match outcome and do not write that CF.

The user's config is authoritative: a CF listed in config and found once by name belongs to
Recyclarr. MUST NOT reintroduce a manual adoption step.

## Rejected Options

- Name normalization (lowercase before comparing): still picks arbitrarily between case variants.
- Automatic renaming of conflicting CFs: changes user data Recyclarr does not own.
- Explicit adoption only (`state repair --adopt`): users nearly always wanted adoption, and an
  ad-hoc command is impractical in scheduled or Kubernetes deployments.

## Consequences

- Renames in the service do not break ownership.
- Ambiguous names need manual cleanup in the service before that CF syncs.
- `replace_existing_custom_formats` is removed and reported as an ignored deprecated property.

## History

- 2025-12-15: Switch to ID-first matching; name matches without state require explicit adoption.
- 2026-01-09: Remove `replace_existing_custom_formats`.
- 2026-03-05: Adopt single name matches automatically.
- 2026-09-26: Remove the `state repair` command.
