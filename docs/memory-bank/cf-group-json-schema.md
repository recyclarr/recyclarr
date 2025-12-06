# CF Group JSON Schema

Location: `docs/json/{radarr,sonarr}/cf-groups/*.json`

## Purpose

Organizational grouping of related custom formats. Simplifies configuration by allowing
users to reference a group instead of listing individual trash_ids.

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `trash_id` | string | Unique identifier for the group |
| `name` | string | Display name (e.g., "[Audio] Audio Formats") |
| `trash_description` | string | HTML description |
| `default` | string | "true"/"false" - enabled by default (UI affordance) |
| `custom_formats` | array | List of CFs in this group |
| `quality_profiles` | object | Profile applicability rules |

## Custom Format Entry

```json
{
  "name": "TrueHD Atmos",
  "trash_id": "496f355514737f7d83bf7aa4d24f8169",
  "required": true,
  "default": true  // optional
}
```

### required vs default (from nitsua)

- `required: true` = MUST be synced, user cannot disable. Profile "breaks" without it.
- `default: true` = Enabled by default, user can disable (UI affordance)
- `required: true` takes precedence over `default`

## Profile Applicability

```json
"quality_profiles": {
  "exclude": {
    "HD Bluray + WEB": "d1d67249d3890e49bc12e275d989a7e9",
    "SQP-1 (1080p)": "0896c29d74de619df168d23b98104b22"
  }
}
```

- **Include unless excluded** - intentional design decision
- `exclude` maps profile name → profile trash_id
- No `include` section exists - if profile not in exclude, group applies

## Recyclarr Integration

CF sources (merged for final profile):
1. User's `custom_formats:` in config YAML
2. Profile's `formatItems`
3. CF groups that reference (don't exclude) the profile
