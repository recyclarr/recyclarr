# End-to-End Test Standards

## Overview

E2E tests run the full Recyclarr CLI against containerized Sonarr/Radarr instances. Tests verify
that sync operations produce expected state in the services.

## Running E2E Tests

**MANDATORY**: Use `./scripts/Run-E2ETests.ps1` - never run `dotnet test` directly for E2E tests.
The script outputs a log file path; use `rg` to search logs without rerunning tests.

## Resource Provider Strategy

The test uses multiple resource providers to verify different loading mechanisms:

### Official Trash Guides (Pinned SHA)

```yaml
- name: trash-guides-pinned
  type: trash-guides
  clone_url: https://github.com/TRaSH-Guides/Guides.git
  reference: <pinned-sha>
  replace_default: true
```

**Purpose**: Baseline data that tests real-world compatibility.

**Use for**: Stable CFs that exist in official guides (e.g., `Bad Dual Groups`, `Obfuscated`).

**Why pinned**: Prevents upstream changes from breaking tests unexpectedly.

### Local Custom Format Providers

```yaml
- name: sonarr-cfs-local
  type: custom-formats
  service: sonarr
  path: <local-path>
```

**Purpose**: Tests `type: custom-formats` provider behavior specifically.

**Use for**: CFs that need controlled structure or don't exist in official guides.

### Trash Guides Override

```yaml
- name: radarr-override
  type: trash-guides
  path: <local-path>
```

**Purpose**: Tests override/layering behavior (higher precedence than official guides).

**Use for**:
- Quality profiles with known structure for testing inheritance
- CF groups with controlled members for testing group behavior
- CFs that override official guide CFs (e.g., HybridOverride)

## Fixture Directory Structure

```txt
Fixtures/
  recyclarr.yml              # Test configuration
  settings.yml               # Resource provider definitions
  custom-formats-sonarr/     # type: custom-formats provider (Sonarr)
  custom-formats-radarr/     # type: custom-formats provider (Radarr)
  trash-guides-override/     # type: trash-guides provider (override layer)
    metadata.json            # Defines paths for each resource type
    docs/
      Radarr/
        cf/                  # Custom formats
        cf-groups/           # CF groups
        quality-profiles/    # Quality profiles
      Sonarr/
        cf/
        cf-groups/
        quality-profiles/
```

## When to Use Each Provider Type

### Use Official Guides When

- Testing sync of real-world CFs that are stable
- Testing compatibility with actual guide data structures
- The specific CF content doesn't matter, just that syncing works

### Use Local Fixtures When

- Testing specific inheritance/override behavior
- Testing resources that don't exist in official guides
- Testing provider-specific loading behavior
- You need controlled, predictable resource structure

## Trash ID Conventions

- `e2e00000000000000000000000000001` - E2E test Radarr quality profile
- `e2e00000000000000000000000000002` - E2E test Sonarr quality profile
- `e2e00000000000000000000000000003` - E2E test Sonarr guide-only profile
- `e2e00000000000000000000000000010` - E2E test Sonarr CF group
- `e2e00000000000000000000000000011` - E2E test Radarr CF group
- `00000000000000000000000000000001` through `00000000000000000000000000000007` - Local test CFs

## Adding New Test Cases

1. **For new CFs**: Add JSON to appropriate `custom-formats-*` or `trash-guides-override/docs/*/cf/`
2. **For new QPs**: Add JSON to `trash-guides-override/docs/*/quality-profiles/`
3. **For new CF groups**: Add JSON to `trash-guides-override/docs/*/cf-groups/`
4. **Update metadata.json** if adding new resource type paths
5. **Update recyclarr.yml** to reference the new trash_ids
6. **Update test assertions** in `RecyclarrSyncTests.cs`

## metadata.json Structure

The metadata.json file tells Recyclarr where to find each resource type:

```json
{
  "json_paths": {
    "radarr": {
      "custom_formats": ["docs/Radarr/cf"],
      "qualities": [],
      "naming": [],
      "custom_format_groups": ["docs/Radarr/cf-groups"],
      "quality_profiles": ["docs/Radarr/quality-profiles"]
    },
    "sonarr": { ... }
  }
}
```

**Important**: Paths must not contain spaces. Use `cf` instead of `Custom Formats`.
