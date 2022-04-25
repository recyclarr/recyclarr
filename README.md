# TRaSH Guide Updater

[![MIT license](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/rcdailey/trash-updater/blob/master/LICENSE)
![build status](https://github.com/rcdailey/trash-updater/actions/workflows/build.yml/badge.svg?branch=master)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rcdailey_trash-updater&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=rcdailey_trash-updater)
[![GitHub release](https://img.shields.io/github/release/rcdailey/trash-updater.svg)](https://github.com/rcdailey/trash-updater/releases/)
[![Support Server](https://img.shields.io/discord/492590071455940612.svg?color=7289da&label=TRaSH-Guides&logo=discord&style=flat-square)](https://discord.com/invite/Vau8dZ3)

Automatically mirror TRaSH guides to your Sonarr/Radarr instance.

## Sonarr Features

### Release Profiles

- "Preferred", "Must Not Contain", and "Must Contain" terms from guides are reflected in
  corresponding release profile fields in Sonarr.
- "Include Preferred when Renaming" is properly checked/unchecked depending on explicit mention of
  this in the guides.
- Profiles get created if they do not exist, or updated if they already exist. Profiles get a unique
  name based on the guide and this name is used to find them in subsequent runs.
- Tags can be added to any updated or created profiles. Tags are created for you if they do not
  exist.
- Ability to convert preferred with negative scores to "Must not contain" terms.
- Terms mentioned as "optional" in the guide can be selectively included or excluded; based entirely
  on user preference.

### Quality Definitions

- Anime and Series (Non-Anime) quality definitions from the guide.
- "Hybrid" type supported that is a mixture of both.

## Radarr Features

### Quality Definitions

- Movie quality definition from the guide

### Custom Formats

- A user-specified list of custom formats are synchronized to Radarr from the TRaSH guides.
- Scores from the guides can be synchronized to quality profiles of your choosing.
- User can specify their own scores for custom formats (instead of using the guide score).
- Option to enable automatic deletion custom formats in Radarr when they are removed from config or
  the guide.

## Installation

Simply download the latest release for your platform using the table below. The download itself is
just a ZIP file with a single executable in it. You can put this executable anywhere you want and
run it.

| Platform   | 32-bit           | 64-bit                                 |
| ---------- | ---------------- | -------------------------------------- |
| Windows    | ---              | [x64][win-x64], [arm64][win-arm64]     |
| Linux      | [arm][linux-arm] | [x64][linux-x64], [arm64][linux-arm64] |
| Linux MUSL | [arm][musl-arm]  | [x64][musl-x64], [arm64][musl-arm64]   |
| Mac OS     | ---              | [x64][osx-x64], [arm64][osx-arm64]     |

[win-x64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-win-x64.zip
[win-arm64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-win-arm64.zip
[linux-x64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-x64.zip
[linux-arm64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-arm64.zip
[linux-arm]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-arm.zip
[musl-x64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-musl-x64.zip
[musl-arm64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-musl-arm64.zip
[musl-arm]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-linux-musl-arm.zip
[osx-x64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-osx-x64.zip
[osx-arm64]: https://github.com/rcdailey/trash-updater/releases/latest/download/trash-osx-arm64.zip

The above links are from the latest release on the [releases page][rp]. Feel free to visit there for
release notes and older releases.

### Important Notices

- For Sonarr support to work, you must be running version `3.0.4.1098` or greater.
- Do not run Notifiarr's Trash Guides Integration in conjunction with Trash Updater's Custom Format
  synchronization. In general, you should not have two different tools updating the same data in
  Radarr.

[rp]: https://github.com/rcdailey/trash-updater/releases

### Special Note about Linux

When you extract the ZIP archive on Linux, it will *not* have the executable permission set. After
you've downloaded and extracted the executable, you can use the command below to make it executable.

```bash
chmod u+rx trash
```

*Note: There used to be a convenient one-liner available here, but that was removed with the
introduction of multiple architecture support. That one liner was no longer sufficient and a more
complex solution was needed to determine which architecture to download for. But if you're using
linux, I think you can handle what needs to be done :-)*

### Special Note regarding Ubuntu 22.04

Ubuntu moved from libssl 1.1 to libssl 3.0 in 22.04. This currently breaks the updater. See https://github.com/rcdailey/trash-updater/issues/54

## Getting Started

Trash Updater requires a YAML configuration file in order to work. Run the steps below if you want
to get started with a minimal configuration file.

- Run `trash create-config` to create a starter `trash.yml` file in the same directory as the
  executable. You can also use `--path` to customize the filename and location.
- Open the generated YAML file from the previous step. At a minimum you must update the `base_url`
  and `api_key` settings for each service that you want to use. Change/delete other parts of the
  file as you see fit.
- Run `trash sonarr` and/or `trash radarr` as needed to update those instances.

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
- [Settings Reference](../../wiki/Settings-Reference)
- [Troubleshooting](../../wiki/Troubleshooting)

## Important Notices

The script may stop working at any time due to guide updates. I will do my best to fix them in a
timely manner. Reporting such issues ASAP would be appreciated and will help identify issues more
quickly.

Please be aware that this application relies on a deterministic and consistent structure of the
TRaSH Guide markdown files. I have [documented guidelines][dg] for the TRaSH Guides that should help
to reduce the risk of the guide breaking the program's parsing logic, however it requires that guide
contributors follow them.

[dg]: ../../wiki/TRaSH-Guide-Structural-Guidelines
