# TRaSH Guides CF Groups Discord Discussion

Date: 2025-11-06
Participants: yammes, oakmudsad, TRaSH, nitsua, voidpointer

## Context

Third-party developer (oakmudsad) building TRaSH Guides sync tool encountered confusion about CF
Group JSON structure. Discussion clarified design intent and surfaced historical architecture
decisions.

## TRaSH Guides CF Group Design Principles

### Default Flag Semantics

The `default` field in CF Group JSONs indicates recommendation strength, NOT mutual exclusivity:

- `default: true` - Required or strongly recommended (auto-enabled)
- `default: false` - Optional based on user preferences/hardware

**Critical insight:** TRaSH: "No hdr formats should be excluded for any 4k profile but only one is
enabled by default the rest are optional"

### Relationship Model

Most CF Groups are **additive**, not mutually exclusive. Multiple HDR format groups can coexist
(HDR, DV Boost, HDR10+ Boost, DV w/o HDR fallback).

Actual mutual exclusivity is rare. Example: SDR vs SDR (no WEBDL).

TRaSH: "The written guide gives suggestions and explains in more detail. The JSON is for the
3rd-party sync apps, with some groups enabled by default, but the end user has more options to
choose what they want"

### Profile Application Logic: Exclude vs Include

**Current implementation:** CF Groups use `exclude` lists - groups apply to ALL quality profiles
EXCEPT those explicitly excluded.

**Historical context:** Exclude logic chosen over include during initial implementation. TRaSH
acknowledges include would be easier but decision made by others during personal circumstances.

**Arguments for include (fail-closed model):**

- Explicit intent
- Shorter lists (fewer profiles)
- Prevents unintended application to new profiles

**Arguments for exclude (current):**

- Less maintenance when adding profiles (nitsua)
- New changes "assumed included" vs manually added
- Smaller exclude lists in practice

**Status:** TRaSH prefers include but change blocked pending Notifiarr coordination. No timeline.

## Implications for Recyclarr

### Current State

Yammes: "for recyclarr, we don't need to worry about the jsons at all as they're not being used
yet"

Recyclarr's template-based config provides explicit control superior to both include/exclude models.
Users directly reference CFs in templates, bypassing CF Group auto-application logic entirely.

### Future Considerations

**Config validation (low priority):**

If TRaSH adds relationship metadata, could warn about conflicting selections. Wait for upstream
changes.

**Documentation:**

Clarify that `default: false` indicates user choice based on hardware/preferences, not requirements.

## Key Takeaways

1. `default` field = recommendation strength, not exclusivity metadata
2. CF Groups are predominantly additive/optional, not mutually exclusive
3. Exclude-vs-include debate is historical baggage, deferred indefinitely
4. Recyclarr's template approach already superior - no action needed

## References

- TRaSH CF Groups documentation:
  <https://github.com/TRaSH-Guides/Guides/blob/master/CONTRIBUTING.md#cf-groups>
- Oakmudsad's prototype metadata file: cf-group-relationships.json (not reviewed)
