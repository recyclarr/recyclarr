# Quality Profile State Resolution

> Extends [trash-id-state-system.md](trash-id-state-system.md) for Quality Profiles specifically.
> The base document describes single trash_id matching; this document covers the two-pass algorithm
> needed when multiple profiles share a trash_id.

## Problem

Users want to create multiple service profiles from the same guide resource with different overrides
(e.g., same quality ordering but different score thresholds or upgrade settings). This requires the
same `trash_id` to appear multiple times under `quality_profiles`:

```yaml
quality_profiles:
  - name: Any
    trash_id: ca39fe2fec28ae7a6e8779404b614f80
    upgrade_allowed: true

  - name: Arabic
    trash_id: ca39fe2fec28ae7a6e8779404b614f80
    upgrade_allowed: false
    min_format_score: 100
```

The base ID-first matching algorithm uses `FindId(trash_id)` which returns a single service_id. When
multiple profiles share a trash_id, this lookup is ambiguous.

## Solution: Two-Pass State Resolution

Quality profiles use a composite key `(trash_id, name)` for state lookups instead of `trash_id`
alone. Resolution happens in two passes.

### Definitions (scoped per trash_id)

- **Unclaimed mapping**: a state entry for this trash_id whose service_id was not claimed by any
  planned profile in Pass 1. These are old entries representing previously synced profiles.
- **Unmatched profile**: a planned profile (from current config) with this trash_id that did not
  find an exact `(trash_id, name)` hit in state during Pass 1.

### Pass 1: Exact Match

For each guide-backed planned profile, look up state by `(trash_id, name)`. If found, claim the
mapping's service_id and process as an existing profile update.

### Pass 2: Rename Detection

After all exact matches are resolved, group remaining unmatched profiles by trash_id. For each
group, count unclaimed state mappings:

- **1 unclaimed, 1 unmatched**: unambiguous rename. Claim the mapping and update the existing
  service profile (including its name).
- **All other combinations**: fall through to name collision handling (same as base algorithm:
  create new, report conflict, or report ambiguity).

## Rename Constraint

When N profiles share a trash_id, at most 1 may be renamed between syncs. This constraint exists
because Pass 2 can only unambiguously pair a single unclaimed mapping with a single unmatched
profile.

### What works in a single sync

- Rename 1 of N profiles (others unchanged)
- Add a new profile (no renames)
- Remove a profile (no renames)

### What requires multiple syncs

- Rename 2+ profiles: rename one, sync, rename the next, sync again
- Rename 1 + add new simultaneously: add first (sync), then rename (sync); or rename first (sync),
  then add (sync)
- Remove 1 + rename another simultaneously: remove first (sync), then rename (sync)

### Why the constraint exists

State mappings use `(trash_id, name, service_id)`. Without a user-specified stable identifier beyond
name, the only way to detect a rename is finding exactly one "hole" (unclaimed mapping) and exactly
one "new entry" (unmatched profile) for a given trash_id. Multiple simultaneous changes create
ambiguity about which old mapping corresponds to which new profile.

An alternative would be a user-defined `id` field in YAML, but that adds configuration complexity
for an uncommon operation.

## Walkthroughs

### Rename 1 of 2

State: `[(X, "A", 42), (X, "B", 43)]` Config: `[(X, "A"), (X, "B2")]`

1. Pass 1: "A" exact-matches (X, "A") -> claims 42. "B2" unmatched.
2. Pass 2: trash_id X has 1 unclaimed (43), 1 unmatched ("B2") -> rename.

Result: profile 42 updated as "A", profile 43 renamed to "B2".

### Rename 2 of 2 (creates new profiles)

State: `[(X, "A", 42), (X, "B", 43)]` Config: `[(X, "A2"), (X, "B2")]`

1. Pass 1: neither matches -> both unmatched.
2. Pass 2: 2 unclaimed, 2 unmatched -> ambiguous -> both fall to name collision.

Result: both treated as new profiles. Old profiles 42 and 43 become orphaned.

### Add second instance

State: `[(X, "A", 42)]` Config: `[(X, "A"), (X, "Clone")]`

1. Pass 1: "A" exact-matches -> claims 42.
2. Pass 2: 0 unclaimed, 1 unmatched -> name collision -> creates new profile.

Result: profile 42 updated, "Clone" created as new.

### Rename 1 + add new (ambiguous)

State: `[(X, "A", 42), (X, "B", 43)]` Config: `[(X, "A"), (X, "B2"), (X, "C")]`

1. Pass 1: "A" claims 42. "B2" and "C" unmatched.
2. Pass 2: 1 unclaimed (43), 2 unmatched -> ambiguous -> both fall to name collision.

Result: "B2" and "C" both created as new profiles. Old "B" (43) orphaned. Workaround: rename "B" to
"B2" first (sync), then add "C" (second sync).

### Remove 1 + rename other (ambiguous)

State: `[(X, "A", 42), (X, "B", 43)]` Config: `[(X, "A2")]`

1. Pass 1: "A2" unmatched.
2. Pass 2: 2 unclaimed (42, 43), 1 unmatched -> ambiguous -> falls to name collision.

Result: "A2" created as new. Both old profiles orphaned. Workaround: remove "B" first (sync), then
rename "A" to "A2" (second sync).

## Validation

Duplicate profile names are rejected in the plan phase. All quality profiles must have unique names
after guide name resolution. This catches both the obvious case (two entries with the same explicit
name) and the subtle case (two entries sharing a trash_id where both omit `name` and inherit the
same guide default). When duplicates are detected, all conflicting entries are skipped with an
error.

## Relationship to Other Systems

**State repair**: not affected. State repair uses name-based matching via `TrashIdMappingMatcher`,
not `FindId()`. The composite lookup changes don't alter repair behavior.

**Custom Format pipeline**: not affected. CFs maintain 1:1 trash_id-to-service_id semantics and use
the single-arg `FindId(trash_id)`.
