<!-- markdownlint-configure-file { "MD024": { "siblings_only": true } } -->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

This release contains **BREAKING CHANGES**. See the [Upgrade Guide] for required changes you need to
make.

### Changed

- **BREAKING**: Sonarr Release profiles are now synced based on a "Trash ID" taken from [the sonarr
JSON files][sonarrjson]. This breaks existing `trash.yml` and manual changes *are required*.
- Do not follow HTTP redirects and instead issue a warning to the user that they are potentially
  using the wrong URL.
- Radarr: Sanitize URLs in HTTP exception messages ([#17]).
- Sonarr: Release profiles starting with `[Trash]` but are not specified in the config are deleted.

### Added

- Linux MUSL builds for arm, arm64, and x64. Main target for this was supporting Alpine Linux in
  Docker.
- Sonarr: Ability to include or exclude specific optional Required, Ignored, or Preferred terms in
  release profiles.
- Sonarr: New `--list-release-profiles` command line option which can be used to quickly and
  conveniently get a list of release profiles (and their Trash IDs) so you know what to add in your
  YAML config under `release_profiles`.
- Sonarr: New `--list-terms` command line option which can be used get a list of terms for a release
  profile. These lists of terms can be used to include or exclude specific optionals, for example.

[#17]: https://github.com/rcdailey/recyclarr/issues/17
[Upgrade Guide]: https://github.com/rcdailey/recyclarr/wiki/Upgrade-Guide
[sonarrjson]: https://github.com/TRaSH-/Guides/tree/master/docs/json/sonarr

## [1.8.2] - 2022-03-06

### Fixed

- Sonarr: Error when syncing optionals release profile with the `IncludeOptionals` filter setting
  set to `false`.

## [1.8.1] - 2022-03-05

### Changed

- Unrecognized or unwanted YAML properties in configuration YAML (`trash.yml`) now result in an
  error. This is to help users more easily identify mistakes.

### Fixed

- Sonarr: Optionals release profile is now properly synced

## [1.8.0] - 2022-02-13

### Added

- 64-bit ARM builds for Windows, Linux, and Mac OS.
- 32-bit ARM build for Linux.

## [1.7.0] - 2022-02-06

### Added

- New settings file to control non-service specific behavior of Trash Updater. See [the
  documentation][setref] for more information.
- Trash git repository URL can be overridden in settings.
- Schema added for `settings.yml`.
- Add setting to bypass HTTPS certificate validation (useful for self-signed certificates used with
  Sonarr and Radarr instances) ([#20]).
- A progress bar that is visible when pulling down Custom Formats (Radarr Only).

### Fixed

- Remove `System.Reactive.xml` from the published ZIP files.
- Fix exception that may occur at startup.
- Sometimes the "Requesting and parsing guide markdown" step would appear stuck and fail after
  several minutes. Many changes have been made to try to alleviate this.

[setref]: https://github.com/rcdailey/recyclarr/wiki/Settings-Reference
[#20]: https://github.com/rcdailey/recyclarr/issues/20

## [1.6.6] - 2021-10-30

### Fixed

- Sonarr version check failed when instances were slow to respond or there was high latency.

## [1.6.5] - 2021-10-24

### Fixed

- Fix "free-quota limit" exception that occurred in new JSON schema generation logic that was added
  for API backward compatibility with Sonarr.

## [1.6.4] - 2021-10-23

### FIXED

- libgit2sharp PDB is no longer required with trash.exe on Windows ([#15])
- Unexpected character error due to breaking change in Sonarr API ([#16])

[#15]: https://github.com/rcdailey/recyclarr/issues/15
[#16]: https://github.com/rcdailey/recyclarr/issues/16

## [1.6.3] - 2021-07-31

- Fix "assembly not found" error on startup related to LibGit2Sharp (Windows only). Note that this
  introduces an additional file in the released ZIP files named `git2-6777db8.pdb`. This file must
  be next to `trash.exe`. In the future, I plan to have this extra file removed so it's just a
  single executable again, but it will take some time.

## [1.6.2] - 2021-07-23

### Fixed

- Directly use the Trash Guides git repository to avoid getting HTTP 403 - rate limit reached error
  in github.

## [1.6.1] - 2021-05-31

### Changed

- Sonarr: Use new URL for release profile guide.
- Sonarr: Use new URL for quality definition guide.
- Radarr: Use new URL for quality definition guide.

## [1.6.0] - 2021-05-31

### Added

- New setting `reset_unmatched_scores` under `custom_formats.quality_profiles` in YAML config which
  allows Trash Updater to set scores to 0 if they were not in the list of custom format names or
  listed but had no score applied (e.g. no score in guide).

### Changed

- Support the new custom format structure in the guide: JSON files are parsed directly now. Trash
  Updater no longer parses the markdown file.

## [1.5.1] - 2021-05-26

### Changed

- Support `trash_score` property in Custom Format JSON from the guide. This property is optional and
  takes precedence over a score mentioned in the guide markdown.

## [1.5.0] - 2021-05-16

### Added

- Custom formats can now be specified by Trash ID. This is useful for situations where two or more
  custom formats in the guide have the same name (e.g. 'DoVi').
- Debug-level logs are now written to file in addition to the Info-level logs in console output.

### Fixed

- An issue with radarr `--preview` that caused duplicate output when updating a second instance has
  been fixed.

## [1.4.2] - 2021-05-15

### Fixed

- Fixed using incorrect URL for Sonarr

## [1.4.1] - 2021-05-15

### Fixed

- Invalid cache data files no longer cause the program to exit. An error is printed and the
  application continues as if there was no cache at all.
- Fix a bug that resulted in certain custom formats not having their scores set in quality
  profiles.
- Fixed an issue where multiple instance configuration was not working.

### Changed

- The log message listing custom formats without scores in the guide now prints information one per
  line (improved readability)
- Duplicate custom formats in the guide now issue a warning and get skipped.
- Do not invoke the Radarr API to update a quality profile if there are no updated scores inside it.

## [1.4.0] - 2021-05-14

### Added

- Radarr Custom Format Support.

## [1.3.3] - 2021-05-06

### Fixed

- Sonarr Quality Definition Max, when set to its maximum value of 400, is now properly set to
  "Unlimited". This is equivalent to the user maxing out the visual slider in the Sonarr UI. Without
  this, some larger sized releases were unintentionally rejected.

## [1.3.2] - 2021-05-05

### Fixed

- Fix exception that occurred when running the create-config subcommand.

## [1.3.1] - 2021-05-05

### Changed

- Executable is now compiled using [Ready to Run]. This substantially increases the size of the
  executable but makes the code much faster.

### Fixed

- Radarr Quality Definition Max and Preferred, when set to their maximum values, are now properly
  set to "Unlimited". Without this, larger sized releases were unintentionally rejected.

[Ready to Run]: https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run

## [1.3.0] - 2021-04-23

### Added

- New configuration for Sonarr release profiles that allows all optional terms to be synced. Look
  for `filter` in the [Configuration Reference] for more details.

[Configuration Reference]: https://github.com/rcdailey/recyclarr/wiki/Configuration-Reference

## [1.2.0] - 2021-04-19

### Added

- New `create-config` subcommand to create a starter YAML config file

## [1.1.0] - 2021-04-18

### Added

- Optional terms in the release profile guides are no longer synchronized to Sonarr.

### Changed

- A warning is now logged when we find a number in brackets (such as `[100]`) without the word
  `score` before it. This represents a potential score and bug in the guide itself.
- Release profile guide parser now skips certain lines to avoid false positives:
  - Skip lines with leading whitespace (i.e. indented lines).
  - Skip admonition lines (lines starting with `!!!` or `???`).

## [1.0.0] - 2021-04-14

See the [Python Migration Guide][py-mig] for details on how to update your YAML configuration.

[py-mig]: https://github.com/rcdailey/recyclarr/wiki/Python-Migration-Guide

### Added

- Full rewrite of the application in C# .NET Core 5
- More than one configuration (YAML) file can be specified using the `--config` option.
- Multiple Sonarr and Radarr instances can be specified in a single YAML config.

### Removed

- Nearly all command line options removed in favor of YAML equivalents.
- Completely removed old python project & source code

[Unreleased]: https://github.com/rcdailey/recyclarr/compare/v1.8.2...HEAD
[1.8.2]: https://github.com/rcdailey/recyclarr/compare/v1.8.1...v1.8.2
[1.8.1]: https://github.com/rcdailey/recyclarr/compare/v1.8.0...v1.8.1
[1.8.0]: https://github.com/rcdailey/recyclarr/compare/v1.7.0...v1.8.0
[1.7.0]: https://github.com/rcdailey/recyclarr/compare/v1.6.6...v1.7.0
[1.6.6]: https://github.com/rcdailey/recyclarr/compare/v1.6.5...v1.6.6
[1.6.5]: https://github.com/rcdailey/recyclarr/compare/v1.6.4...v1.6.5
[1.6.4]: https://github.com/rcdailey/recyclarr/compare/v1.6.3...v1.6.4
[1.6.3]: https://github.com/rcdailey/recyclarr/compare/v1.6.2...v1.6.3
[1.6.2]: https://github.com/rcdailey/recyclarr/compare/v1.6.1...v1.6.2
[1.6.1]: https://github.com/rcdailey/recyclarr/compare/v1.6.0...v1.6.1
[1.6.0]: https://github.com/rcdailey/recyclarr/compare/v1.5.1...v1.6.0
[1.5.1]: https://github.com/rcdailey/recyclarr/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/rcdailey/recyclarr/compare/v1.4.2...v1.5.0
[1.4.2]: https://github.com/rcdailey/recyclarr/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/rcdailey/recyclarr/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/rcdailey/recyclarr/compare/v1.3.3...v1.4.0
[1.3.3]: https://github.com/rcdailey/recyclarr/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/rcdailey/recyclarr/compare/v1.3.1...v1.3.2
[1.3.1]: https://github.com/rcdailey/recyclarr/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/rcdailey/recyclarr/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/rcdailey/recyclarr/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/rcdailey/recyclarr/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/rcdailey/recyclarr/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/rcdailey/recyclarr/releases/tag/v0.1.0
