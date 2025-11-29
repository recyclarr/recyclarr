# Quality Profile JSON Schema

Location: `docs/json/{radarr,sonarr}/quality-profiles/*.json`

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `trash_id` | string | Unique identifier for the profile |
| `name` | string | Display name in Radarr/Sonarr |
| `trash_description` | string | HTML description for documentation |
| `trash_score_set` | string | Key for CF score lookup (e.g., `sqp-1-2160p`) |
| `group` | int | Sorting weight for TRaSH Guides website display (groups related profiles) |
| `upgradeAllowed` | bool | Whether upgrades are permitted |
| `cutoff` | string | Quality name where upgrades stop |
| `minFormatScore` | int | Minimum CF score threshold |
| `cutoffFormatScore` | int | Score threshold for upgrade cutoff |
| `minUpgradeFormatScore` | int | Minimum score improvement for upgrade |
| `language` | string | Language setting (typically "Original") |
| `items` | array | Quality hierarchy (see below) |
| `formatItems` | object | Map of CF name → trash_id |

## Quality Items Structure

```json
{ "name": "Bluray-1080p", "allowed": true }

// Grouped qualities:
{
  "name": "WEB 1080p",
  "allowed": true,
  "items": ["WEBDL-1080p", "WEBRip-1080p"]
}
```

## Score Resolution

1. User specifies `score:` in config → use that
2. Profile has `trash_score_set` → lookup `cf.trash_scores[score_set]`
3. Fallback → `cf.trash_scores.default` (via `cf.DefaultScore`)

## Recyclarr Integration

- `formatItems` provides implicit trash_ids (like user specifying in `custom_formats:`)
- Profile synced by `trash_id` reference (new) or `name` match (existing behavior)
- Quality items define the quality hierarchy including grouped qualities
- `cutoff` references quality name - must match exactly (user rename = broken matching)
