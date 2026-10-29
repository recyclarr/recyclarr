# PDR-005: CF Group Opt-In Semantics

- **Upstream:** Discord discussion with a TRaSH Guides maintainer (2026-01-20)

## Context

TRaSH Guides CF groups use `required` and `default` flags on each custom format to state sync
behavior. A guide maintainer clarified the intended behavior with annotated screenshots:

1. **Group-level `default: true`**: Group is auto-enabled for matching quality profiles
2. **Group-level no `default`**: Group is opt-in (user must explicitly add it)
3. **CF-level `required: true`**: Always synced when group is enabled (no individual toggle)
4. **CF-level `required: false` + `default: true`**: Pre-selected by default, user can override
5. **CF-level `required: false` + no `default`**: NOT selected by default, user must opt-in

Examples from that clarification:

- **[Audio] Audio Formats**: group `default: true`, all CFs `required: true` → all-or-nothing sync
- **[Optional] Miscellaneous**: no group default, all CFs `required: false` → user picks
  individually
- **[Optional] Golden Rule UHD**: group `default: true`, but CFs are `required: false` with only
  `x265 (no HDR/DV)` having `default: true` → only that CF pre-selected, `x265 (HD)` requires opt-in

## Decision

Within an enabled CF group, custom formats sync by their guide flags:

| CF flags                            | Synced by default | User override                    |
|-------------------------------------|-------------------|----------------------------------|
| `required: true`                    | Always            | None; cannot be excluded         |
| `required: false` + `default: true` | Yes               | `exclude` removes it             |
| `required: false` + no `default`    | No                | `select` or `select_all` adds it |

Modifiers on a `custom_format_groups.add[]` entry:

- `select`: non-default CFs to add on top of the required and default CFs.
- `select_all`: add every CF in the group. Mutually exclusive with `select`.
- `exclude`: default CFs to remove. Listing a required CF is a validation error.

```yaml
custom_format_groups:
  add:
    # Swap the default CF for its mutually exclusive alternative
    - trash_id: ff204bbcecdd487d1cefcefdbf0c278d # [Optional] Golden Rule UHD
      exclude:
        - 839bea857ed2c0a8e084f3cbdbd65ecb # x265 (no HDR/DV)
      select:
        - dc98083864ea246d05a42df0d05f81cc # x265 (HD)

    # Pick optional CFs from a group with no defaults
    - trash_id: 9337080378236ce4c0b183e35790d2a7 # [Optional] Miscellaneous
      select:
        - 0d91270a7255a1e388fa85e959f359d8 # FreeLeech
        - 182fa1c42a2468f8488e6dcf75a81b81 # INTERNAL
```

## Rejected Options

- Every CF in a group by default, with removal only: contradicts the maintainers' intent that
  non-default CFs are opt-in.
- `select` replacing the defaults: adding one optional CF silently dropped the defaults.

## Consequences

- Config: `custom_format_groups.add[].select`, `select_all`, and `exclude`
- Migration: not required
- "Required" means always, "default" means pre-selected, neither means opt-in.

## History

- 2026-01-20: Opt-in semantics, with `select` replacing the default CFs.
- 2026-02-20: `select` becomes additive; add `exclude` for default CFs.
- 2026-02-25: Add `select_all`.
