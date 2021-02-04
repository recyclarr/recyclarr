# TRaSH Guide Updater Script

Automatically mirror TRaSH guides to your *darr instance.

> **NOTICE**: This is a work-in-progress Python script

## Features

Features list will continue to grow. See the limitations & roadmap section for more details!

* Preferred, Must Not Contain, and Must Contain lists from guides are reflected completely in
  corresponding fields in release profiles in Sonarr.
* "Include Preferred when Renaming" is properly checked/unchecked depending on explicit mention of
  this in the guides.
* Profiles get created if they do not exist, or updated if they already exist. Profiles get a unique
  name based on the guide and this name is used to find them in subsequent runs.

## Requirements

* Python 3
* The following packages installed with `pip`:
  * `requests`
  * `packaging`
* For Sonarr updates, you must be running version `3.0.4.1098` or greater.

## Getting Started

I plan to add more tutorials/details/instructions later, but for now just run `trash.py --help`:

```txt
usage: trash.py [-h] [--preview] [--debug] base_uri api_key

Automatically mirror TRaSH guides to your *darr instance.

positional arguments:
  base_uri    The base URL for your Sonarr instance, for example `http://localhost:8989`.
  api_key     Your API key.

optional arguments:
  -h, --help  show this help message and exit
  --preview   Only display the processed markdown results and nothing else.
  --debug     Display additional logs useful for development/debug purposes
```

## Important Notices

Please be aware that this script relies on a deterministic and consistent structure of the TRaSH
Guide markdown files. I'm in the process of creating a set of rules/guidelines to reduce the risk of
the guide breaking this script, but in the meantime the script may stop working at any time due to
guide updates. I will do my best to fix them in a timely manner. Reporting such issues would be
appreciated and will help identify issues more quickly.

### Limitations & Roadmap

This script is a work in progress. At the moment, it only supports the following features:

* Only Sonarr is supported (Radarr will come in the future)
* Only the [Sonarr Anime Guide][1] is supported (more guides to come)
* Better and more polished error handling (it's pretty minimal right now)

[1]: https://trash-guides.info/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime/
