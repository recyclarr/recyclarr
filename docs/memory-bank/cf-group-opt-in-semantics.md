# CF Group Opt-In Semantics - Session Memory Dump

Date: 2026-01-20

## Summary

This session covered two main topics:
1. Building a template conversion script for config-templates repo
2. Clarifying CF group opt-in semantics with TRaSH via Discord

## Part 1: Template Conversion Script

Created `scripts/convert-template.py` in config-templates repo to automate conversion from explicit
CF lists to guide-backed resources.

### Script Capabilities

- **Dry-run mode**: Shows analysis with colored output
  - CFs grouped by source (formatItems, CF groups, orphans)
  - Coverage percentages for each CF group
  - Overlap detection between 100%-matched groups
  - Missing required CFs for partial matches

- **YAML generation**: Outputs converted template
  - Uses `quality_profiles.trash_id` for profile definition
  - Uses `custom_format_groups` for CF groups
  - Preserves header comments and updates date

### Key Design Decisions

**CF Group Matching Logic:**
- Match all groups independently (don't remove CFs from pool)
- 100% required match = group is usable
- <100% required match = orphan (for human review)
- Overlapping CFs between multiple 100%-matched groups = error, requires resolution

**Handling Enabled vs Disabled CFs:**
- Commented CFs in template are parsed with `enabled=False`
- Disabled CFs generate `exclude` entries in matched groups
- Groups with all CFs disabled are output as commented

## Part 2: CF Group Opt-In Semantics

### Background

Original Recyclarr implementation included ALL CFs in a group by default, requiring `exclude` to
remove unwanted CFs. Discussion with TRaSH clarified this doesn't match intended behavior.

### TRaSH's Intended Behavior (Confirmed via Screenshots)

**Group-level flags:**
- `default: true` on group -> Group auto-enabled for matching profiles
- No `default` on group -> Group is opt-in (user must add it)

**CF-level flags within an enabled group:**

| `required` | `default` | Behavior                            |
|------------|-----------|-------------------------------------|
| `true`     | -         | Always synced, no individual toggle |
| `false`    | `true`    | Synced by default, user CAN exclude |
| `false`    | missing   | NOT synced, user must opt-in        |

### Examples from TRaSH's Screenshots

1. **[HDR Formats] HDR**
   - Group: `default: true`
   - HDR CF: `required: true`
   - Result: Always synced when group enabled

2. **[Audio] Audio Formats**
   - Group: `default: true`
   - All 14 CFs: `required: true`
   - Result: All-or-nothing sync

3. **[HDR Formats] DV Boost**
   - Group: NO default (opt-in)
   - DV Boost CF: `required: true`
   - Result: User adds group to get CF

4. **[Optional] Miscellaneous**
   - Group: NO default
   - All CFs: `required: false`, no defaults
   - Result: User picks individually, nothing synced automatically

5. **[Required] Golden Rule UHD**
   - Group: `default: true`
   - x265 (HD): `required: false`, no default
   - x265 (no HDR/DV): `required: false`, `default: true`
   - Result: Only x265 (no HDR/DV) syncs by default; user can swap

6. **[Required] Golden Rule HD**
   - Same structure but OPPOSITE defaults
   - x265 (HD): `required: false`, `default: true`
   - x265 (no HDR/DV): `required: false`, no default

### YAML Implementation Options

**Option A: Include + Exclude**
```yaml
# Swap x265 (no HDR/DV) for x265 (HD)
- trash_id: golden-rule-uhd
  include:
    - x265-hd        # add this (wasn't default)
  exclude:
    - x265-no-hdr    # remove this (was default)
```

**Option B: Select (preferred)**
```yaml
# Use x265 (HD) instead of x265 (no HDR/DV)
- trash_id: golden-rule-uhd
  select:
    - x265-hd        # just this one
```

### Why Option B is Better

1. **Simpler swap pattern**: One line instead of two
2. **Clearer semantics**: "select" implies choosing, not adding
3. **Handles mutually exclusive CFs**: Empty groups with no defaults force user to choose
4. **Addresses yammes' concern**: [Optional] SDR with two mutually exclusive CFs (SDR vs SDR no
   WEBDL) - user can't accidentally enable both

### Behavior Summary for Option B

- No modifiers -> sync required + default CFs
- `select` -> sync required + ONLY these specific optional CFs (replaces defaults)
- `exclude` -> sync defaults MINUS these specific CFs
- `select` and `exclude` can be used together if needed

## PDR Created

Created `docs/decisions/product/005-cf-group-opt-in-semantics.md` capturing:
- Context from TRaSH discussion
- Decision to implement opt-in semantics
- YAML syntax with `select` and `exclude`
- Validation rules

## Open Questions for TRaSH

1. Does the understanding of CF group behavior match his intent?
2. Which YAML approach (Option A or B) feels more intuitive?
3. For mutually exclusive CFs like x265 variants - would separate groups be cleaner?

## Related Files

- `config-templates/scripts/convert-template.py` - Conversion script
- `recyclarr/docs/decisions/product/005-cf-group-opt-in-semantics.md` - PDR
- `recyclarr/docs/reference/trash-guides-cf-groups-discord-2025-11-06.md` - Prior Discord discussion

## Next Steps

1. Wait for TRaSH's feedback on Option A vs B
2. Update PDR based on feedback
3. Implement chosen approach in Recyclarr
4. Update conversion script to generate correct YAML syntax
