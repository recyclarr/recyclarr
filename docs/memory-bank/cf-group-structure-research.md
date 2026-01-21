# TRaSH Guides CF Group Structure Research

**Date:** 2026-01-20
**Scope:** Analysis of 20 Radarr and 16 Sonarr CF groups
**Focus:** How `required` and `default` flags are used in practice

## Quick Reference

### Pattern Distribution
- **All required** (14 groups): Audio codecs, streaming services → mandatory together
- **All opt-in** (10 groups): Movie versions, misc features → user selects specific ones
- **Mixed** (4 groups): Golden Rule groups → mutually-exclusive variants
- **Single CF** (11 groups): HDR boosts, region-specific streaming → standalone options

### Key Finding
**NO group has `required=true, default=true`** on individual CFs. Groups marked `group_default: true` are only recommendations, not auto-included.

---

## Five Patterns Found

### Pattern 1: All CFs `required: true` (All-or-Nothing)
Examples: Audio Formats (14 CFs), Streaming Services General (18-19 CFs)
- User includes group → gets ALL complementary features
- These are feature categories where all items desired equally

### Pattern 2: `required: false, default: true` (Pre-selected but Excludable)
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

---

## Structural Insight

**Every group has homogeneous CF patterns:**
- All required together, OR
- All optional independently, OR
- Two optional variants (mutually-exclusive pair)

**No group has:** "3 required + 2 default + 5 opt-in"

This means **groups represent coherent user choices**, not arbitrary CF collections.

---

## Option A vs B Analysis

### Option A: `include`/`exclude`
```yaml
custom_formats:
  include: [audio-formats, streaming-general]
  exclude: [some-cf]
```

**Problem for opt-in groups:**
```yaml
include: [miscellaneous]  # includes all 14
exclude:
  - bad-dual-groups
  - black-white-editions
  - ... (11 more)  # Must exclude 11 to get 3
```

### Option B: `select` (CF-level selection)
```yaml
custom_formats:
  - name: miscellaneous
    select: [HFR, Multi, VP9]  # Just want these 3
```

**Result:** Option B is superior for 35% of groups

---

## For Implementation Planning

- **Mutual exclusivity:** Need to be documented at group structure level, not just guide text
- **Opt-in groups:** Should default to showing "select from group" UI, not "include/exclude" UI
- **All-required groups:** Can work with both option A and B (select: all, or select: [all])
- **Default behavior:** Should NOT auto-select any CFs (no `default: true` on individual CFs in guides)

---

## Raw Data Summary

**Radarr Groups (20 total):**
- 7 all-optional (Miscellaneous, Movie Versions, SDR, Release Groups French, Release Groups German, Misc SQP, Streaming Misc)
- 5 all-required (Audio, Streaming Asian, Streaming Dutch, Streaming General, Streaming UK)
- 2 mixed (Golden Rule HD, Golden Rule UHD)
- 6 single CF (HDR variants, SQP, Anime)

**Sonarr Groups (16 total):**
- 3 all-optional (Miscellaneous, SDR, Streaming Misc)
- 6 all-required (Audio, Streaming Asian, Streaming Dutch, Streaming French, Streaming General, Streaming UK)
- 2 mixed (Golden Rule HD, Golden Rule UHD)
- 5 single CF (HDR variants, Season Packs)
