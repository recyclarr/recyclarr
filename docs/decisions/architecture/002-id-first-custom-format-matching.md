# ADR-002: ID-First Custom Format Matching

- **Status:** Accepted
- **Date:** 2025-12-15

## Context and Problem Statement

Custom Format synchronization requires matching CFs from TRaSH Guides to existing CFs in
Sonarr/Radarr. The original implementation used name-first matching: find a service CF by name
(case-insensitive), then fall back to cached ID. Issue #672 revealed Sonarr uses case-sensitive
comparison, allowing multiple CFs with case-variant names ("HULU", "Hulu", "hulu") to coexist.
Recyclarr's case-insensitive matching returned arbitrary matches, causing "Must be unique" API
errors.

## Decision Drivers

- IDs are stable after creation; names can change
- Cache already tracks ownership via trash_id to service_id mappings
- Name collisions should be explicit errors, not silent misbehavior
- Adoption should be intentional (`cache rebuild --adopt`), not implicit

## Considered Options

1. Switch to ID-first matching with name-based conflict detection
2. Name normalization (lowercase before comparison)
3. Auto-conflict resolution (rename conflicting CFs automatically)
4. Keep name-first with better collision detection

## Decision Outcome

Chosen option: "Switch to ID-first matching", because IDs provide stable, unambiguous matching while
names serve only for display and conflict detection.

The matching algorithm:

1. Cache entry exists + service CF with that ID exists: UPDATE (regardless of name)
2. Cache entry exists + service CF deleted: check name collision, then CREATE or ERROR
3. No cache entry + name exists in service: ERROR (suggest `cache rebuild --adopt`)
4. No cache entry + name doesn't exist: CREATE

Additionally, deprecate `replace_existing_custom_formats` - its purpose is now handled explicitly
via `cache rebuild --adopt`.

### Consequences

- Good, because O(1) ID lookups replace O(n) name scanning
- Good, because name collisions produce clear error messages instead of silent misbehavior
- Good, because adoption becomes an intentional user action
- Bad, because users with corrupted/missing cache entries must run `cache rebuild`
- Bad, because `replace_existing_custom_formats` is deprecated (breaking change for users relying on
  it)
