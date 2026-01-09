---
name: trash-guides
description: Navigate TRaSH Guides local workspace clone for custom formats, quality profiles, naming, and quality sizes
---

# TRaSH Guides Repository

Local workspace clone at `guides/` (relative to workspace root).

## Directory Structure

All JSON resources are under `docs/json/`:

```
docs/json/
  radarr/
    cf/                    # Custom format JSONs (one per CF)
    cf-groups/             # CF group definitions
    quality-profiles/      # Quality profile definitions
    quality-size/          # Quality definition sizes
    naming/                # Media naming schemes
  sonarr/
    cf/
    cf-groups/
    quality-profiles/
    quality-size/
    naming/
```

## Key Insight

Sonarr and Radarr have SEPARATE custom format definitions with different trash_ids. Audio/HDR/codec
CFs have different IDs per service. A common misconfiguration is using Radarr trash_ids in Sonarr
configs (or vice versa).

## Common Operations

Find a custom format by trash_id:

```bash
rg "trash_id_value" guides/docs/json/radarr/cf/
rg "trash_id_value" guides/docs/json/sonarr/cf/
```

List all quality profiles for a service:

```bash
ls guides/docs/json/sonarr/quality-profiles/
```

Search for a CF by name:

```bash
rg -l "DV HDR10" guides/docs/json/radarr/cf/
```

Check git history for removed/renamed CFs:

```bash
git -C guides log --all -p -S "trash_id_value" -- docs/json/
```
