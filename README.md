# TRaSH Guide Updater Script

Automatically mirror TRaSH guides to your *darr instance.

> **NOTICE**: This is a work-in-progress Python script

## Features

Features list will continue to grow. See the limitations & roadmap section for more details!

* Sonarr Release Profiles
  * Preferred, Must Not Contain, and Must Contain lists from guides are reflected completely in
    corresponding fields in release profiles in Sonarr.
  * "Include Preferred when Renaming" is properly checked/unchecked depending on explicit mention of
    this in the guides.
  * Profiles get created if they do not exist, or updated if they already exist. Profiles get a unique
    name based on the guide and this name is used to find them in subsequent runs.
  * Tags can be added to any updated or created profiles.
* Sonarr Quality Definitions
  * Anime and Non-Anime quality definitions are now synced to Sonarr

## Requirements

* Python 3
* The following packages installed with `pip`:
  * `requests`
  * `packaging`
  * `pyyaml`
* For Sonarr updates, you must be running version `3.0.4.1098` or greater.

To install all of the above required packages, here's a convenient copy & paste one-liner:

```txt
pip install requests packaging pyyaml
```

## Getting Started

The only script you will need to be using is `src/trash.py`. If you've cloned my repository, simply
`cd` to the `src` directory so you can run `trash.py` directly:

```txt
PS E:\code\TrashUpdater\src> .\trash.py -h
usage: trash.py [-h] {profile,quality} ...

Automatically mirror TRaSH guides to your Sonarr/Radarr instance.

optional arguments:
  -h, --help         show this help message and exit

subcommands:
  Operations specific to different parts of the TRaSH guides

  {profile,quality}
    profile          Pages of the guide that define profiles
    quality          Pages in the guide that provide quality definitions
```

The command line is structured into a series of subcommands that each handle a different area of the
guides. For example, you use a separate subcommand to sync quality definitions than you do release
profiles. Simply run `trash.py [subcommand] -h` to get help for `[subcommand]`, which can be any
supported subcommand listed in the top level help output.

### Examples

Some command line examples to show you how to use the script for various tasks. Note that most
command line options were generated on a Windows environment, so you will see OS-specific syntax
(e.g. backslashes). Obviously Python works on Linux systems too, so adjust the examples as needed
for your platform.

To preview what release profile information is parsed out of the Anime profile guide:

```txt
.\trash.py profile sonarr:anime --preview
```

To sync the anime release profiles to your Sonarr instance:

```txt
.\trash.py profile sonarr:anime --base-uri http://localhost:8989 --api-key a95cc792074644759fefe3ccab544f6e
```

To preview the Anime quality definition data parsed out of the Quality Definitions (file sizes) page
of the TRaSH guides:

```txt
.\trash.py quality sonarr:anime --preview
```

Sync the non-anime quality definition to Sonarr:

```txt
.\trash.py quality sonarr:non-anime --base-uri http://localhost:8989 --api-key a95cc792074644759fefe3ccab544f6e
```

## Configuration File

By default, `trash.py` will look for a configuration file named `trash.yml` in the same directory as
the script itself. This configuration file may be used to store your Sonarr and Radarr Base URI and
API Key, which should make using the command line interface a bit less clunky.

```yml
sonarr:
  base_uri: http://localhost:8989
  api_key: a95cc792074644759fefe3ccab544f6e
```

Note that this file is not required to be present. If it is not present, then you will be required
to specify the `--base-uri` and `--api-key` on the command line if it is needed.

Lastly, there's a `--config-file` argument you can use to point to your own YAML config file if you
don't like the where the default one is located.

## Important Notices

Please be aware that this script relies on a deterministic and consistent structure of the TRaSH
Guide markdown files. I'm in the process of creating a set of rules/guidelines to reduce the risk of
the guide breaking this script, but in the meantime the script may stop working at any time due to
guide updates. I will do my best to fix them in a timely manner. Reporting such issues would be
appreciated and will help identify issues more quickly.

### Limitations

This script is a work in progress. At the moment, it only supports the following features and/or has
the following limitations:

* Only Sonarr is supported (Radarr will come in the future)
* Only the [Sonarr Anime Guide][1] is supported (more guides to come)
* Multiple scores on the same line are not supported. Only the first is used.

[1]: https://trash-guides.info/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime/

### Roadmap

In addition to the above limitations, the following items are planned for the future.

* Better and more polished error handling (it's pretty minimal right now)
* Add a way to convert preferred with negative scores to "Must not contain" terms.
* Implement some sort of guide versioning (e.g. to avoid updating a release profile if the guide did
  not change).
* Unit Testing

## Development / Contributing

### Prerequisites

Some additional packages are required to run the unit tests. All can be installed via `pip`:

* `pytest`
