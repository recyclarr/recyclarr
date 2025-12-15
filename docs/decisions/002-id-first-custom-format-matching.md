# ADR-002: ID-First Custom Format Matching

## Status

Accepted

## Context

Custom Format synchronization requires matching CFs from TRaSH Guides to existing CFs in
Sonarr/Radarr. The original implementation used name-first matching: find a service CF by name
(case-insensitive), then fall back to cached ID if no name match exists.

This design assumed Sonarr/Radarr enforced case-insensitive name uniqueness. Issue #672 revealed
this assumption was incorrect: Sonarr uses case-sensitive comparison (`f.Name == c` in C#), allowing
multiple CFs with case-variant names like "HULU", "Hulu", and "hulu" to coexist.

When a user had multiple case-variant CFs, Recyclarr's case-insensitive `FirstOrDefault` returned an
arbitrary match. Attempting to update the wrong CF triggered "Must be unique" API errors.

## Decision

Switch to ID-first matching in the sync transaction phase. The cached service ID is the primary
matching key; name matching is only used for error detection (conflicts and ambiguity).

The matching algorithm:

1. Cache entry exists + service CF with that ID exists → UPDATE (regardless of name)
2. Cache entry exists + service CF deleted → check name collision → CREATE or ERROR
3. No cache entry + name exists in service → ERROR (suggest `cache rebuild --adopt`)
4. No cache entry + name doesn't exist → CREATE

Additionally, deprecate the `replace_existing_custom_formats` configuration option. Its purpose
(adopting manually-created CFs) is now handled explicitly via `cache rebuild --adopt`.

## Rationale

- IDs are stable after creation; names can be changed by users or updated in guides
- The cache already tracks ownership via trash_id → service_id mappings; trusting this is simpler
- Name collisions become explicit errors rather than silent misbehavior
- Adoption becomes an intentional user action (`cache rebuild --adopt`) rather than implicit
- Ambiguous matches (multiple case-variant CFs) are detectable and reportable
- O(1) ID lookups replace O(n) name scanning with `FirstOrDefault`

## Alternatives Considered

### Name Normalization

Normalize CF names (e.g., to lowercase) before comparison to handle case variants deterministically.

Rejected because:

- Doesn't solve the fundamental problem: which of multiple case-variant CFs should be matched?
- Would require storing normalized names in cache, adding complexity
- Doesn't address the ownership tracking problem

### Auto-Conflict Resolution

Automatically rename conflicting CFs in the service to make space for the guide CF.

Rejected because:

- "Don't try to be clever" - auto-fixes create unpredictable behavior
- Users may have legitimate reasons for their CF naming
- Better to error and let users resolve intentionally
- Insight from Notifiarr maintainer: "stop chasing edge cases with auto-fixes"

### Keep Name-First with Better Collision Detection

Detect multiple case-variant matches during name-first matching and error when ambiguous.

Rejected because:

- Still relies on name matching as primary strategy
- The cache already provides a more reliable matching key
- Doesn't address the implicit adoption problem with `replace_existing_custom_formats`

## Consequences

- Users with corrupted or missing cache entries must run `cache rebuild` to re-establish mappings
- The `replace_existing_custom_formats` option is deprecated and has no effect
- Users who want to adopt existing CFs must explicitly run `cache rebuild --adopt`
- Name collisions that previously caused silent misbehavior now produce clear error messages
- The mental model is cleaner: IDs are for matching, names are for display and conflict detection
