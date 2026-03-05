# ADR-002: ID-First Custom Format Matching

- **Status:** Superseded (adoption philosophy changed; see below)
- **Date:** 2025-12-15

## Context and Problem Statement

Custom Format synchronization requires matching CFs from TRaSH Guides to existing CFs in
Sonarr/Radarr. The original implementation used name-first matching: find a service CF by name
(case-insensitive), then fall back to stored ID. Issue #672 revealed Sonarr uses case-sensitive
comparison, allowing multiple CFs with case-variant names ("HULU", "Hulu", "hulu") to coexist.
Recyclarr's case-insensitive matching returned arbitrary matches, causing "Must be unique" API
errors.

## Decision Drivers

- IDs are stable after creation; names can change
- State already tracks ownership via trash_id to service_id mappings
- Name collisions should be explicit errors, not silent misbehavior
- Adoption should be intentional (`state repair --adopt`), not implicit

## Considered Options

1. Switch to ID-first matching with name-based conflict detection
2. Name normalization (lowercase before comparison)
3. Auto-conflict resolution (rename conflicting CFs automatically)
4. Keep name-first with better collision detection

## Decision Outcome

Chosen option: "Switch to ID-first matching", because IDs provide stable, unambiguous matching while
names serve only for display and conflict detection.

The matching algorithm:

1. State entry exists + service CF with that ID exists: UPDATE (regardless of name)
2. State entry exists + service CF deleted: check name collision, then CREATE or ERROR
3. No state entry + name exists in service: ERROR (suggest `state repair --adopt`)
4. No state entry + name doesn't exist: CREATE

Additionally, deprecate `replace_existing_custom_formats` - its purpose is now handled explicitly
via `state repair --adopt`.

### Consequences

- Good, because O(1) ID lookups replace O(n) name scanning
- Good, because name collisions produce clear error messages instead of silent misbehavior
- Good, because adoption becomes an intentional user action
- Bad, because users with corrupted/missing state entries must run `state repair`
- Bad, because `replace_existing_custom_formats` is deprecated (breaking change for users relying on
  it)

## Superseded: Adoption Philosophy

The ID-first matching algorithm from this ADR remains in effect. However, the explicit adoption
requirement (Case 3: "No state + name exists -> ERROR, suggest `--adopt`") has been replaced with
automatic adoption.

**New behavior**: Config is authoritative. If a resource is in the user's YAML config and a single
matching name exists in the service, Recyclarr adopts it automatically with a warning. The `state
repair --adopt` workflow is no longer needed. Only ambiguous matches (2+ service resources with the
same name) still require manual resolution.

**Rationale**: The explicit adoption requirement caused significant friction, especially in
Kubernetes cron job deployments where adhoc CLI commands are impractical. Support experience showed
users nearly universally wanted adoption; the conservative approach was protecting against a
scenario that the happy path (ID-based updates) didn't protect against anyway.
