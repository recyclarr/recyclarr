# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2021-04-14

See the [Python Migration Guide][py-mig] for details on how to update your YAML configuration.

[py-mig]: https://github.com/rcdailey/trash-updater/wiki/Python-Migration-Guide

### Added

- Full rewrite of the application in C# .NET Core 5
- More than one configuration (YAML) file can be specified using the `--config` option.
- Multiple Sonarr and Radarr instances can be specified in a single YAML config.

### Removed

- Nearly all command line options removed in favor of YAML equivalents.
- Completely removed old python project & source code

## [0.1.0]

First (and final) release of the Python version of the application.

<!-- Release Links -->
[unreleased]: https://github.com/rcdailey/trash-updater/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/rcdailey/trash-updater/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/rcdailey/trash-updater/releases/tag/v0.1.0
