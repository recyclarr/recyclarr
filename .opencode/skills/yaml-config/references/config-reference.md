# Config Reference (recyclarr.yml)

Schemas: `schemas/config-schema.json` + `schemas/config/*.json`.

## Complete Working Example

This example shows every major feature. Most configs use a subset of this.

```yaml
# yaml-language-server: $schema=https://schemas.recyclarr.dev/latest/config-schema.json

radarr:
  movies:
    base_url: !secret radarr_url
    api_key: !secret radarr_apikey
    delete_old_custom_formats: true

    quality_definition:
      type: movie

    # Guide-backed profile: trash_id pulls qualities, CFs, scores from guide
    quality_profiles:
      - trash_id: d1d67249d3890e49bc12e275d989a7e9  # HD Bluray + WEB
        reset_unmatched_scores:
          enabled: true

    # Override a specific CF score from the guide default
    custom_formats:
      - trash_ids:
          - b6832f586342ef70d9c128d40c07b872  # Bad Dual Groups
        assign_scores_to:
          - name: HD Bluray + WEB
            score: -10000

    # Add an optional CF group
    custom_format_groups:
      add:
        - trash_id: f4a0410a1df109a66d6e47dcadcce014  # [Optional] Movie Versions

    media_naming:
      folder: default
      movie:
        rename: true
        standard: default

    media_management:
      propers_and_repacks: do_not_prefer

sonarr:
  tv:
    base_url: !secret sonarr_url
    api_key: !secret sonarr_apikey

    quality_definition:
      type: series

    quality_profiles:
      - trash_id: 72dae194fc92bf828f32cde7744e51a1  # WEB-1080p
        reset_unmatched_scores:
          enabled: true

    media_naming:
      series: default
      season: default
      episodes:
        rename: true
        standard: default
```

## Manual Profile Example

When no TRaSH Guide profile fits, build one from scratch using `name` instead of `trash_id`. You
must configure qualities, CFs, and scores yourself.

```yaml
quality_profiles:
  - name: My Custom Profile
    reset_unmatched_scores:
      enabled: true
    upgrade:
      allowed: true
      until_quality: Bluray-1080p
      until_score: 10000
    min_format_score: 0
    qualities:
      - name: Bluray-1080p
      - name: WEB 1080p
        qualities:              # nested list = quality group
          - WEBDL-1080p
          - WEBRip-1080p
      - name: Bluray-720p

custom_formats:
  - trash_ids:
      - e7718d7a3ce595f289bfee26adc178f5  # Repack/Proper
    assign_scores_to:
      - name: My Custom Profile
        score: 5
```

## Profile Variants Example

Same `trash_id` used twice to create two profiles with different settings. Every variant MUST have
an explicit `name`.

```yaml
quality_profiles:
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80
    name: Any
    upgrade:
      allowed: true

  - trash_id: ca39fe2fec28ae7a6e8779404b614f80
    name: Arabic
    upgrade:
      allowed: false
    min_format_score: 100
```

At most one variant can be renamed per sync.

## CF Groups Examples

```yaml
# Skip a default group you don't want
custom_format_groups:
  skip:
    - 9d5acd8f1da78dfbae788182f7605200  # Audio Formats

# Add non-default group, select all CFs, exclude specific ones
custom_format_groups:
  add:
    - trash_id: f4a0410a1df109a66d6e47dcadcce014  # [Optional] Miscellaneous
      select_all: true
      exclude:
        - c9eafd50846d299b862ca9bb6ea91950  # x265

# Target a specific profile (required for user-defined profiles)
custom_format_groups:
  add:
    - trash_id: f737e18b5824d6ebb2d57b957ae2fd6c  # Streaming Services (UK)
      assign_scores_to:
        - name: HD-1080p
```

## Quality Definition with Overrides

```yaml
quality_definition:
  type: movie
  preferred_ratio: 0.5           # 0.0-1.0, interpolates between min and max
  qualities:                     # override specific qualities; rest use guide values
    - name: Bluray-1080p
      min: 5
      max: 100
      preferred: 80
    - name: WEBDL-1080p
      max: unlimited             # keyword, not a number
      preferred: unlimited
```

Constraint: `min` <= `preferred` <= `max`.

## Include Files

Include YAML starts at instance level (no service type or instance name wrapper). Cannot include
`base_url`, `api_key`, or nested `include`.

```yaml
# In your main config:
include:
  - config: my-cfs.yml          # local file (absolute or relative to includes/)
  - template: my-template-id    # from resource provider's includes.json

# my-cfs.yml (include file -- no sonarr/radarr wrapper):
custom_formats:
  - trash_ids:
      - abc123
    assign_scores_to:
      - name: HD-1080p
```

## Value Substitution

```yaml
# secrets.yml (in app data directory):
radarr_url: http://localhost:7878
radarr_apikey: abc123

# In config -- explicit reference:
base_url: !secret radarr_url
api_key: !secret radarr_apikey

# Implicit secrets: omit base_url/api_key entirely if secrets.yml has
# <instance_name>_base_url and <instance_name>_api_key

# Environment variables:
base_url: !env_var RADARR_URL
api_key: !env_var RADARR_KEY fallback_value  # spaces OK, quotes stripped
```

---

## Property Details

Notation: (R) required, (O) optional, (CR) conditionally required.

### Instance-Level Properties

```yaml
base_url: string                # (R) must start with http:// or https://
api_key: string                 # (R) from service Settings > General > Security
delete_old_custom_formats: bool # (O) default: false; only deletes Recyclarr-managed CFs
```

### `quality_definition`

```yaml
type: string                    # (R) use `list qualities` to find valid types
preferred_ratio: number         # (O) 0.0-1.0; default: guide values
qualities:                      # (O) per-quality overrides; default: guide values
  - name: string                # (R) must match guide quality name
    min: number                 # (O) MB/minute, >= 0
    max: number|"unlimited"     # (O) MB/minute, >= 0
    preferred: number|"unlimited" # (O) MB/minute, >= 0
```

### `quality_profiles[]`

Each item requires `name` OR `trash_id` (or both).

```yaml
trash_id: string                # (CR) guide profile ID; auto-syncs qualities, CFs, scores
name: string                    # (CR) profile identity or name override; default: guide name
score_set: string               # (O) default: "default"
min_format_score: number        # (O) default: unchanged in service
min_upgrade_format_score: number # (O) default: unchanged in service
quality_sort: top|bottom        # (O) default: top
reset_unmatched_scores:
  enabled: boolean              # (R) set unmanaged CF scores to 0
  except: [string]              # (O) CF names excluded from reset
upgrade:
  allowed: boolean              # (R) Upgrades Allowed checkbox
  until_quality: string         # (CR) required when qualities list is provided
  until_score: number           # (O) default: unchanged in service
qualities:                      # (CR) required for new profiles
  - name: string                # (R) quality or group name
    enabled: boolean            # (O) default: true
    qualities: [string]         # (O) nested list = quality group
```

Listed order = priority order. Disabled qualities still affect cutoff. Use `enabled: false` (safer)
rather than omitting to disable a quality.

### `custom_formats[]`

```yaml
trash_ids: [string]             # (R) hex hash IDs from TRaSH Guides; unique
assign_scores_to:               # (O) profile score assignments
  - name: string                # (CR) use name OR trash_id, never both
    trash_id: string            # (CR) guide profile ID; single match only
    score: integer              # (O) override; default: guide score
```

### `custom_format_groups`

Default groups auto-sync for guide-backed profiles. Use `skip`/`add` to customize.

```yaml
skip: [string]                  # (O) group trash IDs to exclude from auto-sync
add:
  - trash_id: string            # (R) group ID from guide
    assign_scores_to:           # (O) default: all matching guide-backed profiles
      - name: string            # (CR) use name OR trash_id, never both
        trash_id: string        # (CR) guide profile ID
    select: [string]            # (O) non-default CF trash IDs to add
    select_all: boolean         # (O) default: false; include all non-required CFs
    exclude: [string]           # (O) CF trash IDs to remove from sync set
```

Constraints:

- `select` and `select_all` are mutually exclusive (validation error if both)
- Required CFs always sync and cannot be excluded
- A CF cannot appear in both `select` and `exclude`
- If same group is in both `skip` and `add`, `add` wins

### `media_naming` (Radarr)

```yaml
folder: string                  # Movie Folder Format key from `list naming radarr`
movie:
  rename: boolean               # Rename Movies checkbox
  standard: string              # Standard Movie Format key
```

### `media_naming` (Sonarr)

```yaml
series: string                  # Series Folder Format key from `list naming sonarr`
season: string                  # Season Folder Format key
episodes:
  rename: boolean               # Rename Episodes checkbox
  standard: string              # Standard Episode Format key
  daily: string                 # Daily Episode Format key
  anime: string                 # Anime Episode Format key
```

All `media_naming` properties are optional; unspecified = not synced.

### `media_management`

```yaml
propers_and_repacks: prefer_and_upgrade|do_not_upgrade|do_not_prefer
```

Optional; default: not synced. Use `do_not_prefer` with CF-based repack handling.

### `include[]`

Each item uses exactly one of:

```yaml
config: string                  # local file path (absolute or relative to includes/)
template: string                # template ID from resource provider's includes.json
```

---

## Include Merge Behavior

Root config always takes precedence over included YAML.

Top-level merge operations:

- `custom_formats` -- Join (by CF trash_id + profile; root overrides included scores)
- `custom_format_groups` -- Join (by group trash_id)
- `quality_profiles` -- Join (by trash_id+name or name alone)
- `quality_definition` -- Union (properties merged, scalars replaced)
- `delete_old_custom_formats` -- Replace
- `media_naming` -- Union (nested mappings union, scalars replace)
- `media_management` -- Union (scalars replace)

Within `quality_profiles` join:

- `upgrade` -- Union (each scalar replaces)
- `qualities` -- Replace (complete list required; high-stakes, not Add)
- `reset_unmatched_scores` -- Union (`except` uses Add)
- All scalars -- Replace

Within `custom_format_groups` join:

- `assign_scores_to`, `select`, `select_all`, `exclude` -- all Replace

---

## Common Mistakes

**Using both `name` and `trash_id` in `assign_scores_to`:**

```yaml
# WRONG: validation error
assign_scores_to:
  - name: HD Bluray + WEB
    trash_id: d1d67249d3890e49bc12e275d989a7e9

# CORRECT: use one or the other
assign_scores_to:
  - name: HD Bluray + WEB
```

**Omitting `name` on profile variants:**

```yaml
# WRONG: validation error when trash_id appears multiple times
quality_profiles:
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80

# CORRECT: every variant needs an explicit name
quality_profiles:
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80
    name: Any
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80
    name: Arabic
```

**Using `trash_id` in `assign_scores_to` with profile variants:**

```yaml
# WRONG: ambiguous when multiple profiles share the trash_id
assign_scores_to:
  - trash_id: ca39fe2fec28ae7a6e8779404b614f80

# CORRECT: use name to target a specific variant
assign_scores_to:
  - name: Arabic
```

**Using `select` and `select_all` together:**

```yaml
# WRONG: mutually exclusive, validation error
custom_format_groups:
  add:
    - trash_id: abc123
      select_all: true
      select:
        - def456

# CORRECT: use select_all with exclude to subtract
custom_format_groups:
  add:
    - trash_id: abc123
      select_all: true
      exclude:
        - def456
```

**Including service type in include files:**

```yaml
# WRONG: include files start at instance level
radarr:
  movies:
    custom_formats: ...

# CORRECT: no service type or instance name wrapper
custom_formats:
  - trash_ids:
      - abc123
```

**Expecting `qualities` to merge via Add across includes:**

```yaml
# The qualities list uses Replace, not Add.
# If you specify qualities in an include AND your root config,
# the root config's list completely replaces the include's list.
# Always provide the full qualities list in one place.
```

**Specifying `upgrade` without `allowed`:**

```yaml
# WRONG: allowed is required when upgrade block exists
upgrade:
  until_quality: Bluray-1080p

# CORRECT:
upgrade:
  allowed: true
  until_quality: Bluray-1080p
```
