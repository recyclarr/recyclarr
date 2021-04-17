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
as executable. Run this from the directory where you want `trash` to be installed.

```bash
 wget -O trash.zip https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-x64.zip \
    && unzip trash.zip && rm trash.zip && chmod +x trash
```

## Getting Started

> **TL;DR**: Run `trash [sonarr|radarr] --help` for help with available command line options. Visit
> [the wiki](https://github.com/rcdailey/trash-updater/wiki) for in-depth documentation about the
> command line, configuration, and other topics.

The `trash` executable provides one subcommand per distinct service. This means, for example, you
can run `trash sonarr` and `trash radarr`. When you run these subcommands, the relevant service
configuration is read from the YAML files.

That's all you need to do on the command line to get the program to parse guides and push settings
to the respective service. Most of the documentation will be for the YAML configuration, which is
what drives the behavior of the program.

### Read the Documentation

Main documentation is located in the wiki. Links provided below for some main topics.

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

### Roadmap

In addition to the above limitations, the following items are planned for the future.

- Better and more polished error handling (it's pretty minimal right now)
- Implement some sort of guide versioning (e.g. to avoid updating a release profile if the guide did
  not change).
