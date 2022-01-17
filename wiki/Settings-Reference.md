This page contains the YAML reference for Trash Updater settings. Settings support was introduced in
version `1.7.0`.

The `settings.yml` file is located in the following locations depending on your platform:

| Platform | Location                                                   |
| -------- | ---------------------------------------------------------- |
| Windows  | `%APPDATA%\trash-updater\settings.yml`                     |
| Linux    | `~/.config/trash-updater/settings.yml`                     |
| MacOS    | `~/Library/Application Support/trash-updater/settings.yml` |

Settings in this file affect the behavior of Trash Updater regardless of instance-specific
configuration for Radarr and Sonarr.

If this file does not exist, Trash Updater will create it for you. Starting out, this file will be
empty and default behavior will be used. There is absolutely no need to touch this file unless you
have a specific reason to. It is recommended that you only add the specific properties for the
customizations you need and leave the rest alone.

# YAML Reference

Table of Contents

- [Repository Settings](#repository-settings)

## Repository Settings

```yml
repository:
  clone_url: https://github.com/TRaSH-/Guides.git
```

- `clone_url`<br>
  A URL compatible with `git clone` that is used to clone the [Trash Guides
  repository][official_repo]. This setting exists for enthusiasts that may want to instead have
  Trash Updater pull data from a fork instead of the official repository.

[official_repo]: https://github.com/TRaSH-/Guides
