This page describes the general file structure used by Recyclarr for its data. Many of these are
platform-specific.

## Application Data Directory

The application data directory is the root location for Recyclarr's files. With the exception of the
main `recyclarr.yml` file, everything that Recyclarr reads or writes, by default, starts with this
path.

| Platform | Location                                  |
| -------- | ----------------------------------------- |
| Windows  | `%APPDATA%\recyclarr`                     |
| Linux    | `~/.config/recyclarr`                     |
| MacOS    | `~/Library/Application Support/recyclarr` |

## Default YAML Configuration File

The default YAML configuration file is `recyclarr.yml` and it is always located next to the
Recyclarr executable.
