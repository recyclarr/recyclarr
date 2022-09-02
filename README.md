# Recyclarr

[![MIT license](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/recyclarr/recyclarr/blob/master/LICENSE)
![build status](https://github.com/recyclarr/recyclarr/actions/workflows/build.yml/badge.svg?branch=master)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=recyclarr_recyclarr&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=recyclarr_recyclarr)
[![GitHub release](https://img.shields.io/github/release/recyclarr/recyclarr.svg)](https://github.com/recyclarr/recyclarr/releases/)
[![Support Server](https://img.shields.io/discord/492590071455940612.svg?color=7289da&label=TRaSH-Guides&logo=discord&style=flat-square)](https://discord.com/invite/Vau8dZ3)

A command-line application that will automatically synchronize recommended settings from the [TRaSH
guides](https://trash-guides.info/) to your Sonarr/Radarr instances.

Formerly named "Trash Updater".

## Features

The following information can be synced to \*arr services from the TRaSH Guides. For a more detailed
features list, see the [Features] page.

[Features]: https://github.com/recyclarr/recyclarr/wiki/Features

**Sonarr**:

- Sync Release Profiles from the guide.
- Sync Quality Definitions (sizes) from the guide.
- Add Tags to Release Profiles.
- Assign scores from the guide to quality profiles.

**Radarr**:

- Sync Custom Formats from the guide.
- Sync Quality Definitions from the guide.
- Assign CF scores to quality profile (manual or use values from the guide).

## Requirements & Notices

Before installing & running Recyclarr, please review the requirements & special notices below.

- Minimum Supported Sonarr Version: `3.0.4.1098`
- Minimum Supported Radarr Version: `3.*`
- Sonarr v4 is **not supported yet**.
- Do not run Notifiarr's Trash Guides Integration in conjunction with Recyclarr's Custom Format
  synchronization. In general, you should not have two different tools updating the same data in
  Radarr or Sonarr.

## Docker Installation

It is recommended to use the Docker Image to install Recyclarr. Using this method, you get to enjoy
an easier installation process without having to worry about things like file locations,
dependencies, etc. The official docker image can be run with:

```sh
docker run ghcr.io/recyclarr/recyclarr
```

See the [Docker wiki page][docker] for more setup details.

[docker]: https://github.com/recyclarr/recyclarr/wiki/Docker

## Manual Installation

Simply download the latest release for your platform using the table below. The download itself is
just a ZIP file with a single executable in it. You can put this executable anywhere you want and
run it.

| Platform   | 32-bit           | 64-bit                                 |
| ---------- | ---------------- | -------------------------------------- |
| Windows    | ---              | [x64][win-x64], [arm64][win-arm64]     |
| Linux      | [arm][linux-arm] | [x64][linux-x64], [arm64][linux-arm64] |
| Mac OS     | ---              | [x64][osx-x64], [arm64][osx-arm64]     |

[win-x64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-win-x64.zip
[win-arm64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-win-arm64.zip
[linux-x64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-linux-x64.zip
[linux-arm64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-linux-arm64.zip
[linux-arm]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-linux-arm.zip
[osx-x64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-osx-x64.zip
[osx-arm64]: https://github.com/recyclarr/recyclarr/releases/latest/download/recyclarr-osx-arm64.zip

The above links are from the latest release on the [releases page][rp]. Feel free to visit there for
release notes, additional architectures and platforms, and older releases.

[rp]: https://github.com/recyclarr/recyclarr/releases

### Prerequisites

- OpenSSL 1.x required on Linux

### Special Note about Linux

When you extract the ZIP archive on Linux, it will *not* have the executable permission set. After
you've downloaded and extracted the executable, you can use the command below to make it executable.

```bash
chmod u+rx recyclarr
```

*Note: There used to be a convenient one-liner available here, but that was removed with the
introduction of multiple architecture support. That one liner was no longer sufficient and a more
complex solution was needed to determine which architecture to download for. But if you're using
linux, I think you can handle what needs to be done :-)*

## Getting Started

Recyclarr requires a YAML configuration file in order to work. Run the steps below if you want to
get started with a minimal configuration file.

- Run `recyclarr create-config` to create a starter `recyclarr.yml` file in the [application data
  directory][appdata]. You can also use `--path` to customize the filename and location.
- Open the generated YAML file from the previous step. At a minimum you must update the `base_url`
  and `api_key` settings for each service that you want to use. Change/delete other parts of the
  file as you see fit.
- Run `recyclarr sonarr` and/or `recyclarr radarr` as needed to update those instances.

The last step above will do a "basic" sync from the guides to Sonarr and/or Radarr. The starter YAML
config is very minimal. See the next section for further reading and links to the wiki for
additional topics and more advanced customization.

Lastly, each subcommand supports printing help on the command line. Simply run `recyclarr --help`
for the main help output and a list of subcommands. You can then see the help for each subcommand by
running `recyclarr [subcommand] --help`, where `[subcommand]` is one of those subcommands (e.g.
`sonarr`)

[appdata]: https://github.com/recyclarr/recyclarr/wiki/File-Structure

### Read the Documentation

Main documentation is located in [the wiki](https://github.com/recyclarr/recyclarr/wiki). Links
provided below for some main topics.

- [Command Line Reference](../../wiki/Command-Line-Reference)
- [Configuration Reference](../../wiki/Configuration-Reference)
- [Settings Reference](../../wiki/Settings-Reference)
- [Troubleshooting](../../wiki/Troubleshooting)

## Important Notices

The script may stop working at any time due to guide updates. I will do my best to fix them in a
timely manner. Reporting such issues ASAP would be appreciated and will help identify issues more
quickly.
