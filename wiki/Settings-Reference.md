This page contains the YAML reference for Recyclarr settings. Settings support was introduced in
version `1.7.0`.

The `settings.yml` file is located in the following locations depending on your platform:

| Platform | Location                                               |
| -------- | ------------------------------------------------------ |
| Windows  | `%APPDATA%\recyclarr\settings.yml`                     |
| Linux    | `~/.config/recyclarr/settings.yml`                     |
| MacOS    | `~/Library/Application Support/recyclarr/settings.yml` |

Settings in this file affect the behavior of Recyclarr regardless of instance-specific configuration
for Radarr and Sonarr.

If this file does not exist, Recyclarr will create it for you. Starting out, this file will be empty
and default behavior will be used. There is absolutely no need to touch this file unless you have a
specific reason to. It is recommended that you only add the specific properties for the
customizations you need and leave the rest alone.

# Schema Validation

A schema file is provided for `settings.yml` to help assist in editing the file. To use it, simply
add the below snippet to the first line in your `settings.yml` file:

```yml
# yaml-language-server: $schema=https://raw.githubusercontent.com/rcdailey/recyclarr/master/schemas/settings-schema.json
```

If you use VS Code to edit your settings file and install the [YAML extension][yaml], it will
suggest properties you can use and show you documentation for each without having to reference this
page.

[yaml]: https://marketplace.visualstudio.com/items?itemName=redhat.vscode-yaml

# YAML Reference

Table of Contents

- [Repository Settings](#repository-settings)

## Global Settings

```yml
enable_ssl_certificate_validation: true
```

- `enable_ssl_certificate_validation`<br>
  If set to `false`, SSL certificates are not validated. This is useful if you are connecting to a
  Sonarr or Radarr instance using `https` and it is set up with self-signed certificates. Note that
  disabling this setting is a **security risk** and should be avoided unless you are absolutely sure
  what you are doing.

## Repository Settings

```yml
repository:
  clone_url: https://github.com/TRaSH-/Guides.git
```

- `clone_url`<br>
  A URL compatible with `git clone` that is used to clone the [Trash Guides
  repository][official_repo]. This setting exists for enthusiasts that may want to instead have
  Recyclarr pull data from a fork instead of the official repository.

[official_repo]: https://github.com/TRaSH-/Guides
