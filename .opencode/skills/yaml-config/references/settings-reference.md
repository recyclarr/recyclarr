# Settings Reference (settings.yml)

Located in app data directory. Optional; Recyclarr creates it if missing. Schema:
`schemas/settings-schema.json` + `schemas/settings/*.json`.

## Complete Working Example

```yaml
# yaml-language-server: $schema=https://schemas.recyclarr.dev/latest/settings-schema.json

enable_ssl_certificate_validation: true
git_path: /usr/local/bin/git

log_janitor:
  max_files: 20

notifications:
  verbosity: normal
  apprise:
    mode: stateful
    base_url: http://localhost:8000
    key: recyclarr
    tags: alerts

resource_providers:
  # Pin official trash-guides to a specific commit
  - name: trash-guides-pinned
    type: trash-guides
    clone_url: https://github.com/TRaSH-Guides/Guides.git
    reference: abc123def
    replace_default: true

  # Local CF directory for custom formats not in guides
  - name: my-radarr-cfs
    type: custom-formats
    service: radarr
    path: /config/custom-formats/radarr
```

## Stateless Apprise Example

```yaml
notifications:
  apprise:
    mode: stateless
    base_url: http://localhost:8000
    urls:
      - https://discord.com/api/webhooks/secret
```

## Property Details

Notation: (R) required, (O) optional, (CR) conditionally required.

### Global

```yaml
enable_ssl_certificate_validation: bool  # (O) default: true; false for self-signed certs
git_path: string                # (O) full path to git executable; default: search PATH
```

### `log_janitor`

```yaml
max_files: integer              # (O) max log files to retain, >= 0; default: 20
```

### `notifications`

```yaml
verbosity: normal|detailed|minimal  # (O) default: normal
apprise:
  mode: stateful|stateless      # (R)
  base_url: string              # (R) Apprise API server URL
  key: string                   # (R in stateful) config key on Apprise server
  tags: string                  # (O in stateful) filter; comma = OR, space = AND
  urls: [string]                # (O in stateless) notification service URLs
```

Verbosity: `normal` = errors + warnings + changes, `detailed` = normal + empty messages, `minimal` =
errors + warnings only.

### `resource_providers[]`

Each provider is git-based (has `clone_url`) or local (has `path`).

```yaml
name: string                    # (R) unique ID; alphanumeric, hyphens, underscores
type: trash-guides|config-templates|custom-formats  # (R)
replace_default: boolean        # (O) default: false; replace implicit official provider
service: radarr|sonarr          # (CR) required for custom-formats type

# Git provider:
clone_url: string               # (R) git clone URL
reference: string               # (O) branch, tag, or SHA; default: version-aware or master

# Local provider:
path: string                    # (R) directory path; relative to app data dir
```

Path expectations by type:

- `trash-guides`, `config-templates` -- directory containing `metadata.json`
- `custom-formats` -- flat directory of CF JSON files

Only one provider per type can have `replace_default: true`.

---

## Common Mistakes

**Missing `service` for custom-formats provider:**

```yaml
# WRONG: validation error
- name: my-cfs
  type: custom-formats
  path: /config/cfs

# CORRECT: service is required for custom-formats type
- name: my-cfs
  type: custom-formats
  service: radarr
  path: /config/cfs
```

**Using stateful properties with stateless mode (or vice versa):**

```yaml
# WRONG: key is for stateful mode, urls is for stateless mode
apprise:
  mode: stateless
  base_url: http://localhost:8000
  key: recyclarr              # ignored in stateless mode

# CORRECT: use urls with stateless
apprise:
  mode: stateless
  base_url: http://localhost:8000
  urls:
    - https://discord.com/api/webhooks/secret
```

**Multiple providers with `replace_default: true` for the same type:**

```yaml
# WRONG: only one provider per type can replace the default
- name: guides-a
  type: trash-guides
  clone_url: https://example.com/a.git
  replace_default: true
- name: guides-b
  type: trash-guides
  clone_url: https://example.com/b.git
  replace_default: true

# CORRECT: only one replaces; additional providers layer on top
- name: guides-primary
  type: trash-guides
  clone_url: https://example.com/a.git
  replace_default: true
- name: guides-overlay
  type: trash-guides
  clone_url: https://example.com/b.git
```
