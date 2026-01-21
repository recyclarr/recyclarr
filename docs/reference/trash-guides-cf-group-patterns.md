# TRaSH Guides CF Group Structure Patterns

> Research conducted 2026-01-20. Analysis of 20 Radarr and 16 Sonarr CF groups. This research
> informed [PDR-005](../decisions/product/005-cf-group-opt-in-semantics.md).

## Quick Reference

### Pattern Distribution

- **All required** (14 groups): Audio codecs, streaming services - mandatory together
- **All opt-in** (10 groups): Movie versions, misc features - user selects specific ones
- **Mixed** (4 groups): Golden Rule groups - mutually-exclusive variants
- **Single CF** (11 groups): HDR boosts, region-specific streaming - standalone options

### Key Finding

**NO group has `required=true, default=true`** on individual CFs. Groups marked `group_default:
true` are only recommendations, not auto-included.

## Five Patterns Found

### Pattern 1: All CFs `required: true` (All-or-Nothing)

Examples: Audio Formats (14 CFs), Streaming Services General (18-19 CFs)

- User includes group - gets ALL complementary features
- These are feature categories where all items desired equally

### Pattern 2: `required: false, default: true` (Pre-selected but Overridable)

Examples: Golden Rule HD/UHD (2 CFs each)

- Extremely rare: only 4 CFs in entire dataset have this
- Semantics: "Recommend this variant, but user can swap to alternative"
- Always paired with mutually-exclusive alternatives

### Pattern 3: `required: false, default: false` (Opt-in Only)

Examples: Miscellaneous (14 CFs), Movie Versions (11 CFs), SDR (2 CFs)

- **35% of Radarr groups, 19% of Sonarr groups**
- User browses and selects specific CFs they want
- No pre-selected defaults

### Pattern 4: Mutually-Exclusive CFs

**5 categories identified:**

1. Golden Rule x265 variants (pick one or none)
2. Optional SDR options (pick zero or one)
3. HDR format boost groups (pick one base format + optionally one boost)
4. Streaming service language groups (pick one regional set)

**Key insight:** Mutual exclusivity is documented in guides, NOT enforced by flags

### Pattern 5: Mixed (Some Default, Some Opt-in)

Examples: Golden Rule groups (2 CFs)

- Always exactly 2 CFs with specific structure
- One `default: true`, one `default: false`
- Semantics: "Prefer this variant, but alternative available"

## Structural Insight

**Every group has homogeneous CF patterns:**

- All required together, OR
- All optional independently, OR
- Two optional variants (mutually-exclusive pair)

**No group has:** "3 required + 2 default + 5 opt-in"

This means **groups represent coherent user choices**, not arbitrary CF collections.

## Design Implications

The `select` syntax was chosen over `include`/`exclude` because of opt-in groups (Pattern 3):

```yaml
# With exclude: must list 11 CFs to remove to get 3
include: [miscellaneous]
exclude: [bad-dual-groups, black-white-editions, ...]  # 11 items

# With select: just specify what you want
- trash_id: miscellaneous
  select: [HFR, Multi, VP9]  # 3 items
```

`select` is superior for 35% of groups (all opt-in pattern).

## Raw Data Summary

**Radarr Groups (20 total):**

- 7 all-optional (Miscellaneous, Movie Versions, SDR, Release Groups French, Release Groups German,
  Misc SQP, Streaming Misc)
- 5 all-required (Audio, Streaming Asian, Streaming Dutch, Streaming General, Streaming UK)
- 2 mixed (Golden Rule HD, Golden Rule UHD)
- 6 single CF (HDR variants, SQP, Anime)

**Sonarr Groups (16 total):**

- 3 all-optional (Miscellaneous, SDR, Streaming Misc)
- 6 all-required (Audio, Streaming Asian, Streaming Dutch, Streaming French, Streaming General,
  Streaming UK)
- 2 mixed (Golden Rule HD, Golden Rule UHD)
- 5 single CF (HDR variants, Season Packs)
