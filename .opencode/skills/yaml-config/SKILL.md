---
name: yaml-config
description: >
  Use when writing, modifying, or reviewing Recyclarr YAML configuration files
  (recyclarr.yml, settings.yml) or include files
---

# Recyclarr YAML Configuration

Two YAML files: `recyclarr.yml` (instance config) and `settings.yml` (global settings).

## Typical Config

```yaml
# yaml-language-server: $schema=https://schemas.recyclarr.dev/latest/config-schema.json
radarr:
  movies:
    base_url: !secret radarr_url
    api_key: !secret radarr_apikey
    delete_old_custom_formats: true

    quality_definition:
      type: movie

    quality_profiles:
      - trash_id: d1d67249d3890e49bc12e275d989a7e9  # HD Bluray + WEB
        reset_unmatched_scores:
          enabled: true
```

Guide-backed profiles (`trash_id`) auto-sync qualities, CFs, scores, and default CF groups from
TRaSH Guides. Manual profiles use `name` instead and require you to configure everything yourself.

## Key Rules

- Profile references (`assign_scores_to`) use `name` OR `trash_id`, never both
- `trash_id` targeting requires exactly one profile with that ID; use `name` for variants (multiple
  profiles sharing a `trash_id`)
- `qualities` list uses Replace merge, not Add; always specify the complete list
- Unspecified properties are left untouched in the service (opt-in sync model)
- Files in `configs/` directory are auto-loaded alongside `recyclarr.yml`
- Include files start at instance level (no `sonarr`/`radarr` wrapper)

## References

Read these for complete property details, working examples, and common mistakes:

- [references/config-reference.md](references/config-reference.md) for recyclarr.yml
- [references/settings-reference.md](references/settings-reference.md) for settings.yml
