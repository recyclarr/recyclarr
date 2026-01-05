# TRaSH Guides CF Groups Discord Discussion

- Date: 2025-11-06 to 2025-12-30
- Participants: yammes, oakmudsad, TRaSH, nitsua, voidpointer, mvanbaak, bakerboy448

## Contents

- [Context](#context)
- [Background](#background)
- [Decisions](#decisions)
  - [Decision 1: Exclude to Include Migration](#decision-1-exclude-to-include-migration)
  - [Decision 2: Quality Profile Ordering](#decision-2-quality-profile-ordering)
  - [Decision 3: CF Conflicts Metadata](#decision-3-cf-conflicts-metadata)
  - [Decision 4: Profile Groups](#decision-4-profile-groups)
- [Implications for Recyclarr](#implications-for-recyclarr)
- [References](#references)

## Context

Third-party developer (oakmudsad) building TRaSH Guides sync tool encountered confusion about CF
Group JSON structure. Discussion clarified design intent, surfaced historical architecture
decisions, and ultimately reached consensus on schema improvements.

## Background

### Default Flag Semantics

The `default` field in CF Group JSONs indicates recommendation strength, NOT mutual exclusivity:

- `default: true` - Required or strongly recommended (auto-enabled)
- `default: false` - Optional based on user preferences/hardware

TRaSH: "No hdr formats should be excluded for any 4k profile but only one is enabled by default the
rest are optional"

### Relationship Model

Most CF Groups are additive, not mutually exclusive. Multiple HDR format groups can coexist (HDR, DV
Boost, HDR10+ Boost, DV w/o HDR fallback).

Actual mutual exclusivity is rare. Examples: SDR vs SDR (no WEBDL), x265 (HD) vs x265 (no HDR).

TRaSH: "The written guide gives suggestions and explains in more detail. The JSON is for the
3rd-party sync apps, with some groups enabled by default, but the end user has more options to
choose what they want"

## Decisions

### Decision 1: Exclude to Include Migration

**Status**: Approved (2025-12-30)

**Problem**: CF Groups use `exclude` lists - groups apply to ALL quality profiles EXCEPT those
explicitly excluded. This was designed by nitsua and voidpointer (2022) assuming more profiles would
be included than excluded.

**Arguments for exclude (original)**:

- Fail-open: new profiles automatically receive CF groups
- Less maintenance: exclude lists are shorter
- nitsua: "It is clear that they apply to everything except a small list of exclusions"

**Arguments for include (reform)**:

- Explicit intent - clearer what profiles receive a group
- Prevents unintended application to new profiles
- TRaSH: "I made a mistake by not adding an exclude for a certain profile, which resulted that the
  users got the wrong group added"
- oakmudsad: `[Anime] Remux-1080p` receives irrelevant regional streaming service groups

**Resolution**: yammes proposed switching to include logic.

- TRaSH: "I find comprehension of exclude logic harder than include logic"
- voidpointer: "I will support you guys in any way I can"
- nitsua: "go for it"

**Outcome**: Switch from exclude to include semantics. Implementation coordinated between TRaSH
Guides and Notifiarr for synchronized deployment.

### Decision 2: Quality Profile Ordering

**Status**: Approved (2025-12-30)

**Problem**: Quality items in JSON are ordered bottom-to-top (matching API response format), but
this is counterintuitive for human maintainers.

**Resolution**:

- yammes: "I think we should switch the order of the qualities anyway as it'll make it much easier
  to maintain."
- voidpointer: "I already have CF group and QP support in recyclarr on master... I will wait to ship
  it until these changes are made."

**Outcome**: Invert quality ordering to top-to-bottom (human-readable). Tooling will reverse before
sending to API.

### Decision 3: CF Conflicts Metadata

**Status**: Approved (2025-12-30)

**Problem**: Some custom formats are mutually exclusive but there's no machine-readable way to
express this. Users can footgun themselves by selecting conflicting CFs.

**Proposal**: nitsua proposed a `conflicts.json` file per service:

```json
[
  {"Format A": "11111", "Format B": "22222"},
  {"Format A": "11111", "Format C": "33333"}
]
```

TRaSH identified known conflicts:

- SDR vs SDR (no WEBDL)
- x265 (HD) vs x265 (no HDR)

**Outcome**: Add `conflicts.json` at `docs/json/radarr/conflicts.json` and Sonarr equivalent. Can be
implemented independently of other changes.

nitsua: "that PR can go into place 'now' and be implemented whenever by the devs without testing and
fixing shit like the other changes will require"

### Decision 4: Profile Groups

**Status**: Merged (2025-12-13)

PR #2561 merged, adding a "profile groups" concept to organize quality profiles into logical
categories: Standard, Anime, French, German, SQP.

This enables third-party sync apps to present organized profile selection.

## Implications for Recyclarr

**Current State**: Recyclarr's CF group implementation exists on master but hasn't shipped.
voidpointer confirmed he will wait for upstream schema changes before releasing.

**Quality Profile Ordering**: No change needed - Recyclarr already uses top-to-bottom ordering
internally and reverses before API calls.

**CF Groups Include Migration**: Will require updating `CfGroupResource` parsing when upstream
migrates from `exclude` to `include`. Coordinated deployment ensures no breaking changes for users.

**CF Conflicts Validation**: When `conflicts.json` becomes available, add validation warnings during
sync when users select conflicting custom formats.

## Key Takeaways

1. `default` field = recommendation strength, not exclusivity metadata
2. CF Groups are predominantly additive/optional, not mutually exclusive
3. Exclude-to-include migration approved, awaiting coordinated implementation
4. Quality ordering inversion approved, tooling handles API format conversion
5. CF conflicts metadata approved as independent enhancement

## References

- TRaSH CF Groups documentation:
  <https://github.com/TRaSH-Guides/Guides/blob/master/CONTRIBUTING.md#cf-groups>
- Profile Groups PR: <https://github.com/TRaSH-Guides/Guides/pull/2561>
- [Product decision](../decisions/product/001-trash-guides-schema-migration-2025.md)
