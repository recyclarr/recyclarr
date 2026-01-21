# PDR-005: CF Group Opt-In Semantics

- **Status:** Accepted
- **Date:** 2026-01-20
- **Upstream:** Discord discussion with TRaSH (2026-01-20)

## Context

TRaSH Guides CF Groups use `required` and `default` flags on individual custom formats to indicate
sync behavior. The original Recyclarr implementation included ALL CFs in a group by default,
requiring users to use `exclude` to remove unwanted CFs. This did not match TRaSH's intended
semantics.

TRaSH clarified the intended behavior via Discord with annotated screenshots:

1. **Group-level `default: true`**: Group is auto-enabled for matching quality profiles
2. **Group-level no `default`**: Group is opt-in (user must explicitly add it)
3. **CF-level `required: true`**: Always synced when group is enabled (no individual toggle)
4. **CF-level `required: false` + `default: true`**: Pre-selected by default, user can override
5. **CF-level `required: false` + no `default`**: NOT selected by default, user must opt-in

Examples from TRaSH's clarification:

- **[Audio] Audio Formats**: group `default: true`, all CFs `required: true` → all-or-nothing sync
- **[Optional] Miscellaneous**: no group default, all CFs `required: false` → user picks
  individually
- **[Required] Golden Rule UHD**: group `default: true`, but CFs are `required: false` with only
  `x265 (no HDR/DV)` having `default: true` → only that CF pre-selected, `x265 (HD)` requires opt-in

## Decision

Implement opt-in semantics for CF group custom formats:

| CF Flags                            | Behavior                                            |
|-------------------------------------|-----------------------------------------------------|
| `required: true`                    | Always synced when group enabled                    |
| `required: false` + `default: true` | Synced by default (can be overridden via `select`)  |
| `required: false` + no `default`    | NOT synced by default (must be explicitly selected) |

YAML syntax uses `select` to choose specific optional CFs:

| Modifier     | Behavior                                                    |
|--------------|-------------------------------------------------------------|
| No modifiers | Sync required + default CFs                                 |
| `select`     | Sync required + ONLY these optional CFs (replaces defaults) |

`select` was chosen over `include`/`exclude` because:

1. **Clearer swap pattern**: Choosing x265 (HD) instead of x265 (no HDR/DV) is one line, not two
2. **Better semantics**: "select" implies choosing from options, not adding to a set
3. **Handles mutually exclusive CFs**: Groups with no defaults force explicit choice
4. **Matches guide structure**: 35% of groups are pure opt-in with no defaults
5. **Simpler API**: One modifier covers all cases; users specify what they want, not what to remove

```yaml
custom_format_groups:
  # All-required group: no modifiers needed, get all CFs
  - trash_id: 9d5acd8f1da78dfbae788182f7605200  # [Audio] Audio Formats

  # Swap default CF for alternative (mutually exclusive CFs)
  # Default: x265 (no HDR/DV) syncs. User wants x265 (HD) instead.
  - trash_id: ff204bbcecdd487d1cefcefdbf0c278d  # [Required] Golden Rule UHD
    select:
      - dc98083864ea246d05a42df0d05f81cc  # x265 (HD) - replaces default

  # Select specific optional CFs from a group where none are default
  - trash_id: 9337080378236ce4c0b183e35790d2a7  # [Optional] Miscellaneous
    select:
      - 7357cf5161efbf8c4d5d0c30b4815ee2  # Obfuscated
      - f537cf427b64c38c8e36298f657e4828  # Scene
```

Validation rules:

- `select` is only meaningful for `required: false` CFs (selecting required CFs is redundant but
  allowed for clarity)

## Affected Areas

- Config: `custom_format_groups[].select` added, `exclude` removed
- Commands: None
- Migration: Required - existing configs using `exclude` must convert to `select`

## Consequences

- CF group behavior matches TRaSH's documented intent
- Users have fine-grained control via `select` to choose specific optional CFs
- Clearer mental model: "required" means required, "default" means pre-selected, neither means
  opt-in
- `select` provides intuitive "pick from menu" UX for groups with many optional CFs
- Simpler API surface: one modifier instead of two
