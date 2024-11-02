<!-- markdownlint-configure-file { "MD024": { "siblings_only": true } } -->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Notifications: New `verbosity` setting for Notifications to control the frequency and content of
  notifications sent after sync operations.

## [7.3.0] - 2024-10-28

### Added

- Notifications support through Apprise

### Fixed

- A `DependencyResolutionException` is no longer raised in some commands (e.g. `list`) (#352).

## [7.2.4] - 2024-09-14

### Fixed

- Custom Formats: Smarter change detection logic for custom formats with language specifications,
  which addresses the issue of some CFs constantly showing as updated during sync even if they
  didn't change.

## [7.2.3] - 2024-09-03

### Changed

- Performance: Reduced the number of API calls to obtain service version information.

### Fixed

- Custom Format: The error "CF field of type False is not supported" no longer occurs when syncing
  some language-specific custom formats (#318).

## [7.2.2] - 2024-08-25

### Fixed

- Quality Definition: Support new quality upper limits for Sonarr (1000) and Radarr (2000). This is
  a backward compatible change, so older versions of Sonarr and Radarr will continue to use the
  correct upper limits.

## [7.2.1] - 2024-08-03

### Fixed

- Quality Definition: Eliminated continuous syncing when no changes are present due to Radarr's
  adjustment of the upper limit for "Preferred" from 395 to 399.

## [7.2.0] - 2024-07-28

### Changed

- The node `quality_profiles` under `custom_formats` was renamed to `assign_scores_to` to
  disambiguate it from the top-level `quality_profiles`. The old name is deprecated until the next
  major release. See [here][qp_rename] for details.
- Quality Definition: Improved information about sync result in console output.

[qp_rename]: https://recyclarr.dev/wiki/upgrade-guide/v8.0/#assign-scores-to

### Fixed

- Incorrect URLs were fixed in the local starter config template.
- Quality Definition: Preferred quality setting would not sync in certain situations (#301).

## [7.1.1] - 2024-07-12

### Changed

- The `--app-data` option is now common to all commands.

### Fixed

- CLI: Commands no longer crash due to a null app data directory variable (#288).

## [7.1.0] - 2024-07-10

### Added

- Sync: In rare circumstances outside of Recyclarr, quality profiles become invalid due to missing
  required qualities. When this happens, users are not even able to save the profile using the
  Sonarr or Radarr UI. Recyclarr now detects this situation and automatically repairs the quality
  profile by re-adding these missing qualities for users. See [this issue][9738].

### Fixed

- CLI: Signal interrupt support for all API calls. Now when you press CTRL+C to gracefully
  exit/cancel Recyclarr, it will bail out of any ongoing API calls.
- CLI: The `--app-data` option works again (#284).

[9738]: https://github.com/Radarr/Radarr/issues/9738

## [7.0.0] - 2024-06-27

This release contains **BREAKING CHANGES**. See the [v7.0 Upgrade Guide][breaking7] for required
changes you may need to make.

[breaking7]: https://recyclarr.dev/wiki/upgrade-guide/v7.0/

### Added

- YAML: New `includes` subdirectory intended to hold only include templates. Relative paths
  specified in the `config` include directive are resolved starting at this new directory. Relative
  paths to include templates located under the `configs` directory is now **DEPRECATED**. See the
  "File Structure" page on the wiki for more details.
- Support the [NO_COLOR] environment variable for all Recyclarr commands (#223).

[NO_COLOR]: https://no-color.org/

### Changed

- **BREAKING**: The app data directory on OSX has changed. It now lives at `~/Library/Application
  Support/recyclarr` instead of `~/.config/recyclarr`. Users will need to run `recyclarr migrate` to
  move the directory (or do it manually).
- **BREAKING**: Removed support for Release Profiles and Sonarr version 3. The new minimum required
  version for Sonarr is v4.0.0.
- CLI: Slightly improved display of version number when using `-v` option.
- CLI: Greatly improved the layout of and information in the local starter YAML configuration that
  Recyclarr generates with the `recyclarr config create` command.

### Fixed

- YAML: Print more useful diagnostics when there's a connectivity problem to a service (e.g.
  incorrect `base_url`).
- YAML: Regression that prevented basic validation of `base_url` & `api_key`.
- CLI: CFs with no Trash ID will no longer be displayed when running the `list custom-formats`
  command (#229).
- Docker: Support running the container in read-only mode (#231).
- Sync: Sometimes CFs weren't deleted even with `delete_old_custom_formats` enabled (#237).

## [6.0.2] - 2023-10-20

### Fixed

- CLI: Some custom formats were not properly categorized when running `list custom-formats`.
- CLI: Continue processing other instances when `ServiceIncompatibilityException` is thrown.
- Media Naming: In order to avoid confusion, the `v3` and `v4` version indicators for certain naming
  format keys has been moved to their own column in the `list` command table.

## [6.0.1] - 2023-10-02

### Fixed

- Media Naming: Sync file naming configuration even if `rename` is not set to `true`.
- Quality Profiles: Validation check added for quality groups with less than 2 qualities.
- Quality Profiles: Fix "Groups must contain multiple qualities" sync error.
- Quality Profiles: Fix "Must contain all qualities" sync error.

## [6.0.0] - 2023-09-29

This release contains **BREAKING CHANGES**. See the [v6.0 Upgrade Guide][breaking6] for required
changes you may need to make.

[breaking6]: https://recyclarr.dev/wiki/upgrade-guide/v6.0/

### Added

- Added Naming Sync (Media Management) for Sonarr v3, Sonarr v4, and Radarr (#179).
- A `list naming` command to show Sonarr and Radarr naming formats available from the guide.

### Changed

- **BREAKING**: Minimum required Sonarr version increased to `3.0.9.1549` (Previous minimum version
  was `3.0.4.1098`).
- **BREAKING**: Old boolean syntax for `reset_unmatched_scores` is no longer supported.

### Fixed

- Status text rendered during git repo updates is no longer shown when `--raw` is used with the
  `list custom-formats` command (#215).

## [5.4.3] - 2023-09-16

### Changed

- Remove INF log that showed a total count of CFs without scores assigned. This log caused a lot of
  confusion in support channels. You can still see a list of CFs without scores in the debug logs.
- Relaxed validation rules for `trash_ids` and `quality_profiles` under `custom_formats`. Both of
  these nodes may now be empty. This is mostly to make commenting out parts of configuration
  templates easier.
- The merge operation for `custom_formats` is now "Join" (previously "Add"). If, for the same
  profile, you "reassign" a different score to a CF, the score now gets updated without having to
  remove the CF from `custom_formats` sections in included YAML files.

## [5.4.2] - 2023-09-14

### Fixed

- Print error information about HTTP 401 instead of "Unable to determine".
- Improved wording of remote service error messages.

### Changed

- `qualities` (inside `quality_profiles`) is now a "Replace" merge operation instead of "Add". This
  means only one YAML file manages the full list of qualities. Either an include does it, or you
  override the full list in your configuration file. There is no longer any combination. See [the
  docs][qualitiesmerge] for more details.

[qualitiesmerge]: https://recyclarr.dev/wiki/behavior/include/#managing-qualities

## [5.4.1] - 2023-09-12

### Fixed

- If the guide data for "Include Custom Format when Renaming" is set to "true", it now syncs that
  correctly instead of always setting to "false" (#213).

## [5.4.0] - 2023-09-11

### Added

- Print date & time log at the end of each completed instance sync (#165).
- Add status indicator when cloning or updating git repos.
- YAML includes are now supported (#175) ([docs][includes]).
- New `--include` option added to `config list templates` to show a list of include templates for
  each service type ([docs][listoption]).

### Changed

- Less-verbose console logging for scoreless custom formats.
- Git repository updates are now parallelized.
- Individual updated, created, and deleted CF logs are now debug severity. This makes the console
  output less verbose when syncing custom formats.

### Fixed

- Service failures (e.g. HTTP 500) no longer cause exceptions (#206).
- Error out when duplicate instance names are used.
- Print score instead of object in duplicate score detection warning

[includes]: https://recyclarr.dev/wiki/yaml/config-reference/include/
[listoption]: http://recyclarr.dev/wiki/cli/config/list/templates/#include

## [5.3.1] - 2023-08-21

### Fixed

- Crash when doing `recyclarr sync` with no `reset_unmatched_scores` present.

## [5.3.0] - 2023-08-21

### Added

- New `delete` command added for deleting one, many, or all custom formats from Radarr or Sonarr.
- Exclusions are now supported under `reset_unmatched_scores`. This is used to prevent score resets
  to specific custom formats. See [the docs][except] for more info.
- New `score_set` property available to each profile defined under the top-level `quality_profiles`
  list. This allows different kinds of pre-defined scores to be chosen from the guide, without
  having to explicitly override scores in your YAML.
- New `--score-sets` option added to `list custom-formats` which lists all score sets that CFs are a
  member of, instead of the CFs themselves.
- New `--raw` option added to `list custom-formats` which omits boilerplate output and formatting.
  Useful for scripting.

### Changed

- Program now exits when invalid instances are specified.
- Scores are now pulled from the `trash_scores` object in the guide's CF json files.

### Deprecated

- `reset_unmatched_scores` has a new syntax. The old syntax [has been deprecated][resetdeprecate].

### Fixed

- If multiple configuration files refer to the same `base_url` (i.e. the same instance), this is now
  an error and the program will exit. To use multiple config templates against a single instance of
  Radarr or Sonarr, you need to manually merge those config files. See [this page][configmerge].

[configmerge]: https://recyclarr.dev/wiki/yaml/config-examples/#merge-single-instance
[except]: https://recyclarr.dev/wiki/yaml/config-reference/#qp-reset-unmatched-scores
[resetdeprecate]: https://recyclarr.dev/wiki/upgrade-guide/v6.0/#breaking-changes

## [5.2.1] - 2023-08-07

### Changed

- Reduce the time it takes to clone the config and trash repositories by performing shallow clones
  (#201).

### Fixed

- Better error message to console when no configuration files are found.
- Allow quality group names to duplicate quality names (#200).

## [5.2.0] - 2023-08-06

### Added

- `base_url` and `api_key` are now optional. These can be implicitly set via secrets that follow a
  naming convention. See the Secrets reference page on the wiki for details.
- Quality Profiles can now be created & synced to Radarr, Sonarr v3, and Sonarr v4 (#144).

### Changed

- Better error messages for manually-specified, non-existent config files.
- More detail in error messages when Radarr/Sonarr API calls respond with HTTP 400 "Bad Data".

### Fixed

- Resolved error during exception message formatting that occurred in some cases (#192).

## [5.1.1] - 2023-06-29

### Fixed

- Clone config template repo when `config create -t` is used.
- Fix error when completely commenting out a YAML configuration file (#190).

## [5.1.0] - 2023-06-26

### Added

- Migration step added to delete old `repo` directory. Run `recyclarr migrate` to use.

### Fixed

- Update default clone URL for trash guides repo to new URL:
  `https://github.com/TRaSH-Guides/Guides.git`.

## [5.0.3] - 2023-06-25

### Fixed

- When using `sync`, continue processing other instances when there's a failure.
- Regression: Perform Sonarr compatibility checks again (#189).

## [5.0.2] - 2023-06-24

### Fixed

- Commenting/uncommenting CFs in configuration YAML no longer causes duplicate CF warnings when
  `replace_existing_custom_formats` is omitted or set to `false` (better caching logic).

## [5.0.1] - 2023-06-23

### Changed

- Recyclarr will now continue if `git fetch` fails for any repos, so long as there is an existing,
  valid clone to use.

### Fixed

- Address regression causing `reset_unmatched_scores: false` to not be respected.
- Do not show deleted custom formats in console output when `delete_old_custom_formats` is set to
  `false`.

## [5.0.0] - 2023-06-22

This release contains **BREAKING CHANGES**. See the [v5.0 Upgrade Guide][breaking5] for required
changes you may need to make.

[breaking5]: https://recyclarr.dev/wiki/upgrade-guide/v5.0

### Added

- The `*.yaml` extension is now accepted for all YAML files (e.g. `settings.yaml`, `recyclarr.yaml`)
  in addition to `*.yml` (which was already supported).
- New `--template` option added to `config create` which facilitates creating new configuration
  files from the configuration template repository.
- New `--force` option added to the `config create` command. This will overwrite existing
  configuration files, if they exist.

### Changed

- API Key is now sent via the `X-Api-Key` header instead of the `apikey` query parameter. This
  lessens the need to redact information in the console.
- **BREAKING**: `replace_existing_custom_formats` now defaults to `false`.
- **BREAKING**: Restructured repository settings.
- Configuration templates repository moved to `recyclarr/config-templates` on GitHub. Corresponding
  settings for this repo as well (see the Settings YAML Reference on the wiki for more details).

### Removed

- **BREAKING**: Array-style instances are no longer supported.
- **BREAKING**: Remove deprecated CLI commands: `radarr`, `sonarr`, and `create-config`.
- **BREAKING**: Removed `reset_unmatched_scores` support under quality profile score section.
- **BREAKING**: Migration steps that dealt with the old `trash.yml` have been removed.

### Fixed

- False-positive duplicate score warnings no longer occur when doing `sync --preview` for the first
  time.

## [4.4.1] - 2023-04-08

### Fixed

- Fixed JSON parsing issue that sometimes occurs when pulling custom formats from Radarr (#178).
- Use correct wiki link in settings.yml template.

## [4.4.0] - 2023-04-06

### Added

- New `replace_existing_custom_formats` property that can be set to `false` to disallow updates to
  existing CFs that Recyclarr never created in the first place. The default is `true`.
- New `quality_profiles` section supported for specifying information about quality profiles. For
  now, this section doesn't do much, but paves the way for quality profile syncing.
- New CLI command: `config list` which lists information about local and template config files.

### Changed

- Log files are restructured. They are now under `logs/cli`.
- Log files are split. There is now a `verbose.log` and `debug.log` for every run. The time stamps
  (in the file name) between the two will be identical.

### Deprecated

- `replace_existing_custom_formats` must be explicitly specified, otherwise you will get a
  deprecation warning. In a future release, the default will change from `true` to `false`. To
  prepare for that, users must explicitly state what behavior they want to avoid unwanted behavior
  in the future. Read more
  [here](https://recyclarr.dev/wiki/upgrade-guide/v5.0#replace-existing-custom-formats).
- `reset_unmatched_scores` is being moved to the `quality_profiles` section; a deprecation message
  will be logged until it is moved. Read more
  [here](https://recyclarr.dev/wiki/upgrade-guide/v5.0#reset-unmatched-scores).

### Fixed

- Deleted custom formats are now included in the log message showing the count of CFs synced.
- An error will now be presented if an invalid option is specified on the CLI.
- Compressed builds are now enabled on MacOS. This means the executable size will be smaller.

## [4.3.0] - 2023-01-22

### Added

- Environment variables may now be used in YAML configuration (#145).

### Fixed

- Exception when there's not configuration for both Sonarr and Radarr together.

## [4.2.0] - 2023-01-13

### Added

- New `list` subcommand for listing information from the guide.
- New `sync` command for syncing all services, specific service types, and/or specific instances.
- New `config` subcommand for performing configuration-specific operations.

### Changed

- The CLI has been completely redesigned to be more consistent and structured (#142).
- Improved preview output for quality sizes, custom formats, and release profiles.

### Deprecated

- The `create-config` subcommand is deprecated and replaced by `config create`.
- The `sonarr` subcommand is deprecated and replaced by `sync sonarr`.
- The `radarr` subcommand is deprecated and replaced by `sync radarr`.

## [4.1.3] - 2023-01-07

### Changed

- Do not print skipped custom formats to console (they are too verbose). If you still want to see
  what was skipped, check the log file for additional debug logs.

### Fixed

- More scenarios were causing custom formats to sometimes not be synced (#160).

## [4.1.2] - 2023-01-06

### Fixed

- Remove unredacted request URI from log files on exception.
- Scores/Custom Formats would not sync under certain conditions (#160).

## [4.1.1] - 2023-01-06

### Changed

- More robust configuration validation logic. You may notice new configuration errors that were not
  there before.

### Fixed

- Custom Formats: Updates that conflict with existing CFs in Sonarr/Radarr are now skipped and a
  warning is printed.
- When changing instance URLs, use new cache data to avoid mismatched custom formats on next sync.

## [4.1.0] - 2022-12-30

### Added

- Better visual separation between processed instances in console output. (#146)
- More information about deleted, skipped, updated, and created CFs in console output. (#159)

### Changed

- Category headers in `--list-custom-formats` output is now formatted as a YAML comment that can be
  copied with the list of Trash IDs.

## [4.0.2] - 2022-12-26

### Changed

- Sort CFs alphabetically in `--list-custom-formats`

### Fixed

- Releases now retain executable permissions on Linux and macOS.
- Sonarr: Do not modify or delete release profiles when using `--preview`

## [4.0.1] - 2022-12-21

### Changed

- Docker: Explicit `init` is no longer required in Docker Compose. It is now built into the image.
- Reduced size of the `recyclarr` executable
- macOS & linux are now released as `tar.xz` archives instead of `zip`.

### Fixed

- Fix CoreCLR / "killed" crash on Apple macOS platforms (#39). This was accomplished by properly
  signing and notarizing Recyclarr and disabling compression.

## [4.0.0] - 2022-12-11

This release contains **BREAKING CHANGES**. See the [v4.0 Upgrade Guide][breaking4] for required
changes you need to make.

[breaking4]: https://recyclarr.dev/wiki/upgrade-guide/v4.0

### Changed

- **BREAKING**: Sonarr `quality_definition` configuration updated to address unexpected changes in
  Sonarr v4 that caused it to stop working. See upgrade guide for details.
- Default for `preferred_ratio` changed from `1.0` to using the values from the guide.

### Removed

- **BREAKING**: Sonarr's `hybrid` quality definition removed.

### Fixed

- Do not warn about empty configuration YAML files when they aren't really empty.

## [3.1.0] - 2022-12-10

### Changed

- Improved logging: theme changes, better exception handling, more detail written to log files.
- Print instance name instead of URL in more places.
- Configuration parsing is more forgiving about errors:
  - If there's a YAML syntax error, skip the file but continue.
  - If there's a validation error, skip only that instance (not the whole file).

### Fixed

- Empty configuration files are skipped if they are empty (warning is printed).

## [3.0.0] - 2022-12-03

This release contains **BREAKING CHANGES**. See the [v3.0 Upgrade Guide][breaking3] for required
changes you need to make.

### Added

- New `configs` subdirectory. Place your `*.yml` config files here and all of them will be
  automatically loaded, as if you provided multiple paths to `--config`. The primary purpose of this
  feature is to support multiple configuration files in Docker. See [the docs][yaml-config]
- Secrets support. You can now store sensitive information from your configuration YAML such as
  `api_key` and `base_url` in a `secrets.yml` file. See [the secrets docs][secrets] for more info.
  Huge thanks to @voltron4lyfe for this one. (#105, #139)
- Named instances are now supported in configuration YAML.
- New optional setting `repository.git_path` may be used to specify the path to a `git` executable.
  If not used, `PATH` will be searched.
- Docker: New `RECYCLARR_CREATE_CONFIG` environment variable which, if set to `true`, will
  automatically create `/config/recyclarr.yml` on container start up. Default is `false`.

### Changed

- **BREAKING**: Recyclarr now requires `git` to be installed on host systems when using manual
  installation. If using Docker, there is no breaking change since git will be bundled with the
  image.
- Deprecated array-style instances in configuration YAML. Read more about this in the v3.0 Upgrade
  Guide.

### Removed

- **BREAKING**: Completely removed support for `names` under `custom_formats` in `recyclarr.yml`.
  Note that this had already been deprecated for quite some time.
- **BREAKING**: The deprecated feature that still allowed you to keep your `recyclarr.yml` next to
the executable has been removed.

### Fixed

- Sonarr: Run validation on Custom Formats configuration, if specified, to check for errors.
- Added more instructions, fixed broken links, and simplified the way you uncomment optional parts
  of the YAML in the starter `recyclarr.yml` template generated by the `create-config` subcommand.

[breaking3]: https://recyclarr.dev/wiki/upgrade-guide/upgrade-guide-v3.0
[yaml-config]: https://recyclarr.dev/wiki/file-structure#directory-configs
[secrets]: https://recyclarr.dev/wiki/reference/secrets-reference

## [2.6.1] - 2022-10-15

### Fixed

- Sonarr: Incorrect VersionException occurred when using mixed versions of Sonarr (v4 & v3)

## [2.6.0] - 2022-10-14

### Added

- For both Sonarr and Radarr, the custom formats in the output of `--list-custom-formats` are now
  grouped by their category, as determined by the tables at the top of the "Collection of custom
  formats" pages in the guide for each service.
- Recyclarr's Docker image is now available on Docker Hub! [Link][dockerhub]

[dockerhub]: https://hub.docker.com/r/recyclarr/recyclarr

### Changed

- Docker: `PUID` and `PGID` no longer cause a failure on container start up.

### Fixed

- Use compact JSON for HTTP request/response body in debug log files. This makes logs much easier to
  scroll through.
- Sonarr: Run version enforcement logic when using CFs instead of RPs.
- A warning is now displayed when the same custom format is assigned multiple times to the same
  quality profile.

## [2.5.0] - 2022-09-11

### Added

- Settings: New `log_janitor` setting that allows you to specify how many log files are kept when
  cleaning up (deleting) old log files. See the [Settings Reference] wiki page for more details.
  (#91)
- Sonarr: Custom Formats can now be synced to Version 4.

### Fixed

- Docker: Fix `/config` permissions when not using bind-mount for the volume. (#111)
- Sonarr: Error message is printed when attempting to use release profiles with Sonarr v4. (#100)

### Security

- Several vulnerabilities addressed (Thanks to @snoopy82481): [CVE-2018-8292], [CVE-2019-0980],
  [CVE-2019-0981], [CVE-2019-0820], [CVE-2019-0657]. (#112)

[CVE-2018-8292]: https://avd.aquasec.com/nvd/cve-2018-8292
[CVE-2019-0980]: https://avd.aquasec.com/nvd/cve-2019-0980
[CVE-2019-0981]: https://avd.aquasec.com/nvd/cve-2019-0981
[CVE-2019-0820]: https://avd.aquasec.com/nvd/cve-2019-0820
[CVE-2019-0657]: https://avd.aquasec.com/nvd/cve-2019-0657
[Settings Reference]: https://recyclarr.dev/wiki/reference/settings-reference

## [2.4.1] - 2022-08-26

### Fixed

- Radarr: Custom formats were always showing up as changed in the logs (#109)

## [2.4.0] - 2022-08-25

### Added

- New `--list-qualities` argument for `sonarr` and `radarr` subcommands that may be used to get a
  list of quality definition types from the guide.

### Changed

- Quality definition data is now pulled from JSON files.

## [2.3.1] - 2022-08-20

### Changed

- Use the new paths for custom format and release profile JSON files in the guide.

## [2.3.0] - 2022-08-14

### Added

- Radarr: New `--list-custom-formats` CLI option for getting a flat list of all CFs in the guide in
  YAML format, ready to copy & paste.
- Docker: New `edge` tag for experimental and potentially unstable builds on `master`. Includes both
  the latest Docker and Recyclarr changes to allow users to try them out before an official release.
- Settings: New `branch` and `sha1` Repository settings. (#27)

### Changed

- JSON Schema added to the config template YAML file.
- `names` list under `custom_formats` in config YAML is now deprecated. Use `trash_ids` to list your
  custom formats instead.
- Docker: The image is now rootless. The `PUID` and `PGID` environment variables are no longer used.
  See the [Docker] wiki page for more details.

### Fixed

- Docker: Resolved errors related to `/tmp/.net` directory not existing.
- An exception that says "Cannot write to a closed TextWriter" would sometimes occur at the end of
  running a command.
- Sonarr: Validate the TRaSH Guide data better to avoid uploading bad/empty data to Sonarr.

## [2.2.1] - 2022-06-18

### Changed

- Radarr: Reword the warning about missing scores for CFs to make it more clear that having no score
  does not prevent CFs from being synced.

### Fixed

- Do not exit when a YAML config has no sonarr or radarr section.
- Sonarr: Invalid release profile JSON files no longer cause the program to exit. Instead, it just
  skips them and prints a warning to the user. (#87)
- Radarr: Do not crash when `quality_profiles` is empty. (#89)
- Settings: Use repo URL after initial clone (#90)

## [2.2.0] - 2022-06-03

### Added

- Docker support! Image name is `ghcr.io/recyclarr/recyclarr`. See the [Docker] wiki page for more
  information.
- Global app data path support via environment variable named `RECYCLARR_APP_DATA`. The path
  specified here will be used as the app data path for every invocation of `recyclarr` as if
  `--app-data` were specified.

### Fixed

- Renamed the "EVO (no WEB-DL)" custom format to "EVO (no WEBDL)" in the config template. (#77)
- Radarr: `delete_old_custom_formats` works again. (#71)
- The `create-config` subcommand now accepts YAML files again (it was taking a directory before,
  which was wrong).

[Docker]: https://recyclarr.dev/wiki/installation/docker

## [2.1.2] - 2022-05-29

### Fixed

- `create-config` would fail with `--path` specified.
- `migrate` no longer fails if the `cache` directory does not exist.

## [2.1.1] - 2022-05-29

### Fixed

- Exception when running `create-config` command.

## [2.1.0] - 2022-05-29

### Added

- New `--app-data` option for overriding the location of the application data directory.
- New `migrate` subcommand which may be used to perform migration steps manually.

### Changed

- The default location for the default YAML file (`recyclarr.yml`) has been changed to the
  [application data directory][appdata]. This is the same location of the `settings.yml` file.
- Automatic migration has been removed. Instead, the `migrate` subcommand should be used.

### Deprecated

- The `recyclarr.yml` file should no longer be located adjacent to the `recyclarr` executable.

### Fixed

- Version information in help output has been fixed.
- If a HOME directory is not available, throw an error to the user (use `--app-data` instead).
- Create `$HOME/.config` (on Linux) if it does not exist.
- Smarter migration logic in the `trash-updater` migration step that does a directory merge instead
  of a straight move. This is designed to fail less in cases such as `recyclarr` directory already
  existing.

[appdata]: https://recyclarr.dev/wiki/file-structure

## [2.0.2] - 2022-05-20

### Fixed

- Sonarr: Fix unexpected missing terms when using filters. (#69)

## [2.0.1] - 2022-05-19

### Fixed

- Sonarr: `strict_negative_scores` works again (broke in v2.0 release)

## [2.0.0] - 2022-05-13

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
- [Migration System] that is responsible for performing one-time upgrade tasks as needed.

[#17]: https://github.com/recyclarr/recyclarr/issues/17
[Upgrade Guide]: https://recyclarr.dev/wiki/upgrade-guide/upgrade-guide-v2.0
[sonarrjson]: https://github.com/TRaSH-/Guides/tree/master/docs/json/sonarr
[Migration System]: https://recyclarr.dev/wiki/migration

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

[setref]: https://recyclarr.dev/wiki/reference/settings-reference
[#20]: https://github.com/recyclarr/recyclarr/issues/20

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

[#15]: https://github.com/recyclarr/recyclarr/issues/15
[#16]: https://github.com/recyclarr/recyclarr/issues/16

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

[Configuration Reference]: https://recyclarr.dev/wiki/reference/config-reference

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

See the Python Migration Guide for details on how to update your YAML configuration.

### Added

- Full rewrite of the application in C# .NET Core 5
- More than one configuration (YAML) file can be specified using the `--config` option.
- Multiple Sonarr and Radarr instances can be specified in a single YAML config.

### Removed

- Nearly all command line options removed in favor of YAML equivalents.
- Completely removed old python project & source code

[Unreleased]: https://github.com/recyclarr/recyclarr/compare/v7.3.0...HEAD
[7.3.0]: https://github.com/recyclarr/recyclarr/compare/v7.2.4...v7.3.0
[7.2.4]: https://github.com/recyclarr/recyclarr/compare/v7.2.3...v7.2.4
[7.2.3]: https://github.com/recyclarr/recyclarr/compare/v7.2.2...v7.2.3
[7.2.2]: https://github.com/recyclarr/recyclarr/compare/v7.2.1...v7.2.2
[7.2.1]: https://github.com/recyclarr/recyclarr/compare/v7.2.0...v7.2.1
[7.2.0]: https://github.com/recyclarr/recyclarr/compare/v7.1.1...v7.2.0
[7.1.1]: https://github.com/recyclarr/recyclarr/compare/v7.1.0...v7.1.1
[7.1.0]: https://github.com/recyclarr/recyclarr/compare/v7.0.0...v7.1.0
[7.0.0]: https://github.com/recyclarr/recyclarr/compare/v6.0.2...v7.0.0
[6.0.2]: https://github.com/recyclarr/recyclarr/compare/v6.0.1...v6.0.2
[6.0.1]: https://github.com/recyclarr/recyclarr/compare/v6.0.0...v6.0.1
[6.0.0]: https://github.com/recyclarr/recyclarr/compare/v5.4.3...v6.0.0
[5.4.3]: https://github.com/recyclarr/recyclarr/compare/v5.4.2...v5.4.3
[5.4.2]: https://github.com/recyclarr/recyclarr/compare/v5.4.1...v5.4.2
[5.4.1]: https://github.com/recyclarr/recyclarr/compare/v5.4.0...v5.4.1
[5.4.0]: https://github.com/recyclarr/recyclarr/compare/v5.3.1...v5.4.0
[5.3.1]: https://github.com/recyclarr/recyclarr/compare/v5.3.0...v5.3.1
[5.3.0]: https://github.com/recyclarr/recyclarr/compare/v5.2.1...v5.3.0
[5.2.1]: https://github.com/recyclarr/recyclarr/compare/v5.2.0...v5.2.1
[5.2.0]: https://github.com/recyclarr/recyclarr/compare/v5.1.1...v5.2.0
[5.1.1]: https://github.com/recyclarr/recyclarr/compare/v5.1.0...v5.1.1
[5.1.0]: https://github.com/recyclarr/recyclarr/compare/v5.0.3...v5.1.0
[5.0.3]: https://github.com/recyclarr/recyclarr/compare/v5.0.2...v5.0.3
[5.0.2]: https://github.com/recyclarr/recyclarr/compare/v5.0.1...v5.0.2
[5.0.1]: https://github.com/recyclarr/recyclarr/compare/v5.0.0...v5.0.1
[5.0.0]: https://github.com/recyclarr/recyclarr/compare/v4.4.1...v5.0.0
[4.4.1]: https://github.com/recyclarr/recyclarr/compare/v4.4.0...v4.4.1
[4.4.0]: https://github.com/recyclarr/recyclarr/compare/v4.3.0...v4.4.0
[4.3.0]: https://github.com/recyclarr/recyclarr/compare/v4.2.0...v4.3.0
[4.2.0]: https://github.com/recyclarr/recyclarr/compare/v4.1.3...v4.2.0
[4.1.3]: https://github.com/recyclarr/recyclarr/compare/v4.1.2...v4.1.3
[4.1.2]: https://github.com/recyclarr/recyclarr/compare/v4.1.1...v4.1.2
[4.1.1]: https://github.com/recyclarr/recyclarr/compare/v4.1.0...v4.1.1
[4.1.0]: https://github.com/recyclarr/recyclarr/compare/v4.0.2...v4.1.0
[4.0.2]: https://github.com/recyclarr/recyclarr/compare/v4.0.1...v4.0.2
[4.0.1]: https://github.com/recyclarr/recyclarr/compare/v4.0.0...v4.0.1
[4.0.0]: https://github.com/recyclarr/recyclarr/compare/v3.1.0...v4.0.0
[3.1.0]: https://github.com/recyclarr/recyclarr/compare/v3.0.0...v3.1.0
[3.0.0]: https://github.com/recyclarr/recyclarr/compare/v2.6.1...v3.0.0
[2.6.1]: https://github.com/recyclarr/recyclarr/compare/v2.6.0...v2.6.1
[2.6.0]: https://github.com/recyclarr/recyclarr/compare/v2.5.0...v2.6.0
[2.5.0]: https://github.com/recyclarr/recyclarr/compare/v2.4.1...v2.5.0
[2.4.1]: https://github.com/recyclarr/recyclarr/compare/v2.4.0...v2.4.1
[2.4.0]: https://github.com/recyclarr/recyclarr/compare/v2.3.1...v2.4.0
[2.3.1]: https://github.com/recyclarr/recyclarr/compare/v2.3.0...v2.3.1
[2.3.0]: https://github.com/recyclarr/recyclarr/compare/v2.2.1...v2.3.0
[2.2.1]: https://github.com/recyclarr/recyclarr/compare/v2.2.0...v2.2.1
[2.2.0]: https://github.com/recyclarr/recyclarr/compare/v2.1.2...v2.2.0
[2.1.2]: https://github.com/recyclarr/recyclarr/compare/v2.1.1...v2.1.2
[2.1.1]: https://github.com/recyclarr/recyclarr/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/recyclarr/recyclarr/compare/v2.0.2...v2.1.0
[2.0.2]: https://github.com/recyclarr/recyclarr/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/recyclarr/recyclarr/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/recyclarr/recyclarr/compare/v1.8.2...v2.0.0
[1.8.2]: https://github.com/recyclarr/recyclarr/compare/v1.8.1...v1.8.2
[1.8.1]: https://github.com/recyclarr/recyclarr/compare/v1.8.0...v1.8.1
[1.8.0]: https://github.com/recyclarr/recyclarr/compare/v1.7.0...v1.8.0
[1.7.0]: https://github.com/recyclarr/recyclarr/compare/v1.6.6...v1.7.0
[1.6.6]: https://github.com/recyclarr/recyclarr/compare/v1.6.5...v1.6.6
[1.6.5]: https://github.com/recyclarr/recyclarr/compare/v1.6.4...v1.6.5
[1.6.4]: https://github.com/recyclarr/recyclarr/compare/v1.6.3...v1.6.4
[1.6.3]: https://github.com/recyclarr/recyclarr/compare/v1.6.2...v1.6.3
[1.6.2]: https://github.com/recyclarr/recyclarr/compare/v1.6.1...v1.6.2
[1.6.1]: https://github.com/recyclarr/recyclarr/compare/v1.6.0...v1.6.1
[1.6.0]: https://github.com/recyclarr/recyclarr/compare/v1.5.1...v1.6.0
[1.5.1]: https://github.com/recyclarr/recyclarr/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/recyclarr/recyclarr/compare/v1.4.2...v1.5.0
[1.4.2]: https://github.com/recyclarr/recyclarr/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/recyclarr/recyclarr/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/recyclarr/recyclarr/compare/v1.3.3...v1.4.0
[1.3.3]: https://github.com/recyclarr/recyclarr/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/recyclarr/recyclarr/compare/v1.3.1...v1.3.2
[1.3.1]: https://github.com/recyclarr/recyclarr/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/recyclarr/recyclarr/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/recyclarr/recyclarr/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/recyclarr/recyclarr/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/recyclarr/recyclarr/compare/v0.1.0...v1.0.0
