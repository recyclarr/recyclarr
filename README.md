# TRaSH Guide Updater

Automatically mirror TRaSH guides to your Sonarr/Radarr instance.

> **NOTICE**: This program is a work-in-progress!

## Features

Features list will continue to grow. See the limitations & roadmap section for more details!

### Sonarr

Release Profiles

- "Preferred", "Must Not Contain", and "Must Contain" terms from guides are reflected in
  corresponding release profile fields in Sonarr.
- "Include Preferred when Renaming" is properly checked/unchecked depending on explicit mention of
  this in the guides.
- Profiles get created if they do not exist, or updated if they already exist. Profiles get a unique
  name based on the guide and this name is used to find them in subsequent runs.
- Tags can be added to any updated or created profiles.
- Ability to convert preferred with negative scores to "Must not contain" terms.
- Terms mentioned as "optional" in the guide are not synced to Sonarr release profiles.

Quality Definitions

- Anime and Series (Non-Anime) quality definitions from the guide.
- "Hybrid" type supported that is a mixture of both.

### Radarr

Quality Definitions

- Movie quality definition from the guide

## Installation

Simply download the latest release for your platform:

- [Windows (64-bit)](https://github.com/rcdailey/trash-updater/releases/latest/download/trash-win-x64.zip)
- [Linux (64-bit)](https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-x64.zip)
- [macOS (64-bit)](https://github.com/rcdailey/trash-updater/releases/latest/download/trash-osx-x64.zip)

The above links are from the latest release on the [releases page][rp]. Feel free to visit there for
release notes and older releases.

> **Note**: For Sonarr updates to work, you must be running version `3.0.4.1098` or greater.

[rp]: https://github.com/rcdailey/trash-updater/releases

### Special Note about Linux

When you extract the ZIP archive on Linux, it will *not* have the executable permission set. Here is
a quick one-liner you can use in a terminal to download the latest release, extract it, and set it
as executable. It will also replace any existing `trash` executable, so this is useful for upgrades,
too. Run this from the directory where you want `trash` to be installed.

```bash
 wget -O trash.zip https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-x64.zip \
    && unzip -o trash.zip && rm trash.zip && chmod +x trash
```

## Getting Started

Trash Updater requires a YAML configuration file in order to work. Run the steps below if you want
to get started with minimal configuration file template.

- Run `trash create-config` to create a starter `trash.yml` file in the same directory as the
  executable. You can also use `--config` to customize the filename and location.
- Open the generated YAML file from the previous step. At a minimum you must update the `base_url`
  and `api_key` settings for each service that you want to use.
- Run `trash sonarr` and/or `trash.radarr` as needed to update those instances.

The last step above will do a "basic" sync from the guides to Sonarr and/or Radarr. The starter YAML
config is very minimal. See the next section for further reading and links to the wiki for
additional topics and more advanced customization.

Lastly, each subcommand supports printing help on the command line. Simply run `trash --help` for
the main help output and a list of subcommands. You can then see the help for each subcommand by
running `trash [subcommand] --help`, where `[subcommand]` is one of those subcommands (e.g.
`sonarr`)

### Read the Documentation

Main documentation is located in [the wiki](https://github.com/rcdailey/trash-updater/wiki). Links
provided below for some main topics.

- [Command Line Reference](../../wiki/Command-Line-Reference)
- [Configuration Reference](../../wiki/Configuration-Reference)

## Important Notices

The script may stop working at any time due to guide updates. I will do my best to fix them in a
timely manner. Reporting such issues ASAP would be appreciated and will help identify issues more
quickly.

Please be aware that this application relies on a deterministic and consistent structure of the
TRaSH Guide markdown files. I have [documented guidelines][dg] for the TRaSH Guides that should help
to reduce the risk of the guide breaking the program's parsing logic, however it requires that guide
contributors follow them.

[dg]: ../../wiki/TRaSH-Guide-Structural-Guidelines

### Limitations

This application is a work in progress. At the moment, it only supports the following features
and/or has the following limitations:

- Radarr custom formats are not supported yet (coming soon).
- Multiple scores on the same line are not supported. Only the first is used.
- There's no way to explicitly include optional terms to be synced to Sonarr.

### Roadmap

In addition to the above limitations, the following items are planned for the future.

- Better and more polished error handling (it's pretty minimal right now)
- Implement some sort of guide versioning (e.g. to avoid updating a release profile if the guide did
  not change).
