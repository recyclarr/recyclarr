# Contributing

Thank you for your interest in contributing to Recyclarr! This document outlines the requirements
and expectations for all contributors.

## Before Contributing

- **Open an issue first**: Always open an issue (feature request) before starting any work. This
  ensures ideas are discussed first and prevents the potential rejection of work already done in a
  pull request. This practice respects your valuable time as an open source contributor.

## Code & Documentation Standards

### Markdown Changes

- All changes must pass configured [markdownlint] rules
- See the `.markdownlint.json` files in this repository for project-specific rule adjustments

### C# Code Requirements

- Code must be free of warnings and analysis issues (zero tolerance policy)
- Code quality is enforced via [Qodana] analysis in CI/CD
- All new code must use the "Recyclarr Cleanup" [Code Cleanup] profile for code quality and
  redundancy fixes
- All new code must be formatted with [CSharpier] for consistent code style
- When using Jetbrains Rider, select the "Recyclarr Cleanup" profile for code cleanup
- Visual Studio users should use the Resharper extension

#### Using CSharpier

There are two recommended ways to use CSharpier:

1. **IDE Integration (preferred)**<br/>
   CSharpier supports [many IDEs and editors][csharpier_plugins]. Configure your plugin to format on
   save for automatic reformatting.

1. **CLI Tool**<br/>
   Install tooling: `dotnet tool restore` at the repo root. Format all C# files with `dotnet
   csharpier .`, or format specific areas with `dotnet csharpier path/to/directory`.

## Required Tools

### Essential

- .NET SDK 9.0 and tooling (including dotnet CLI)
- Powershell v5.1 or greater
- Docker CLI (Docker Desktop on Windows)

### Highly Recommended

- Jetbrains Rider (IDE for editing C# code)
- Visual Studio Code (with workspace-recommended extensions)

Additional required tooling can be installed via the `scripts/Install-Tooling.ps1` powershell
script. Run this occasionally for upgrades as well.

## Conventional Commits

This project uses and enforces [Conventional Commits][commits] with the following commit types:

| Type       | Description                                                   |
| ---------- | ------------------------------------------------------------- |
| `build`    | Update project files, settings, etc.                          |
| `chore`    | Anything not code related or that falls into other categories |
| `ci`       | Changes to CI/CD scripts or configuration                     |
| `docs`     | Updates to non-code documentation (markdown, readme, etc)     |
| `feat`     | A new feature implementation                                  |
| `fix`      | A defect or security issue fix                                |
| `perf`     | Code changes related to improving performance                 |
| `refactor` | Code changes that don't impact observable functionality       |
| `revert`   | Prefix for commits made by the `git revert` command           |
| `style`    | Whitespace or code cleanup changes                            |
| `test`     | Updates to unit test code only                                |

## Docker Development

The project includes utility scripts to simplify development and debugging workflows using Docker,
supporting both local and containerized execution of Recyclarr.

### Debug Services Stack

The root `docker-compose.yml` provides configuration for services used when debugging Recyclarr:

- Radarr (develop and stable builds)
- Sonarr (develop and stable builds)
- Apprise (for testing notifications)
- SQLite (for database inspection)

To start these services:

```powershell
./scripts/Docker-Debug.ps1
```

This runs `docker compose up -d --pull always` to start services in detached mode with the latest images.

Service endpoints are mapped to:

- Radarr develop: <http://localhost:7890>
- Sonarr develop: <http://localhost:8990>
- Apprise: <http://localhost:8000>

### Recyclarr Container

To run Recyclarr inside a Docker container with your local code changes:

```powershell
# Example: Run the sync command
./scripts/Docker-Recyclarr.ps1 sync

# Pass additional arguments to Recyclarr
./scripts/Docker-Recyclarr.ps1 config create --template your-template-name
```

This script:

1. Calls `Docker-Debug.ps1` to ensure dependent services are running
1. Builds a local Recyclarr image with your current code
1. Uses `docker compose run` with the `recyclarr` profile
1. Forwards all arguments to the Recyclarr container
1. Removes the container after completion

The path `docker/debugging/recyclarr` is mapped to `/config` inside the container for configuration
and log access.

> **Note**: By default, the image builds for your local architecture only. Multi-platform builds
> using buildx are outside the scope of this documentation.

## Release Process

Recyclarr follows [Semantic Versioning][semver], using [GitVersion] to automatically version the
executable according to [Conventional Commits][commits] rules.

### Making a Release

1. Run `scripts/Prepare-Release.ps1`, which will:
   - Update the changelog according to [Keep a Changelog][changelog] rules
   - Commit the changelog updates
   - Create a tag for the release (using GitVersion)
1. Push the new tag and commits on `master` to trigger Github Workflows

### Automated Release Process

The Github Workflows handle:

1. Compiling .NET projects
1. Creating a [Github Release][release] with artifacts
1. Building and publishing Docker images to [Github Container Registry][ghcr] and [Docker
   Hub][dockerhub]
1. Sending notifications to the `#related-announcements` channel in the [TRaSH Guides
   Discord][discord]

## Additional Development Tasks

### Updating `.gitignore`

Execute `scripts/Update-Gitignore.ps1` from the repo root. This pulls the latest relevant
`.gitignore` patterns from [gitignore.io](https://gitignore.io) and commits them to your current
branch.

### Testing Discord Notifier

1. Make a GET request to `https://api.github.com/recyclarr/recyclarr/releases/tags/v4.0.0` (or any
   version)

1. Save the response JSON to `ci/notify/release.json`

1. In the `ci/notify` directory, run:

   ```bash
   jq -r '.assets[].browser_download_url' release.json > assets.txt
   jq -r '.body' release.json > changelog.txt
   ```

1. Run the notifier with your test Discord webhook:

   ```bash
   python ./discord_notify.py \
     --version v4.0.0 \
     --repo recyclarr/recyclarr \
     --webhook-url https://discord.com/api/webhooks/your_webhook_url \
     --changelog ./changelog.txt \
     --assets ./assets.txt
   ```

[markdownlint]: https://github.com/DavidAnson/markdownlint
[Qodana]: https://www.jetbrains.com/qodana/
[Code Cleanup]: https://www.jetbrains.com/help/rider/Code_Cleanup__Index.html
[CSharpier]: https://csharpier.com/
[csharpier_plugins]: https://csharpier.com/docs/Editors
[commits]: https://www.conventionalcommits.org/en/v1.0.0/
[semver]: https://semver.org/
[GitVersion]: https://gitversion.net/
[changelog]: https://keepachangelog.com/en/1.0.0/
[release]: https://github.com/recyclarr/recyclarr/releases
[ghcr]: https://github.com/recyclarr/recyclarr/pkgs/container/recyclarr
[discord]: https://discord.com/invite/Vau8dZ3
[dockerhub]: https://hub.docker.com/r/recyclarr/recyclarr
