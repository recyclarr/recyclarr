# Contributing

Thank you for your interest in contributing to Recyclarr! This document outlines the requirements
and expectations for all contributors.

## Contribution workflow

### Design before code

New features, behavior changes, configuration or schema changes, and architectural changes need an
issue with an approved design **before** you open a PR. PRs for these without prior approval will be
closed; this is not personal, and the PR can follow the discussion.

Bug fixes, small enhancements, and documentation fixes can go straight to a PR with a clear
description.

### Review expectations

Maintainers will ask questions about design choices and implementation details during review.
Contributors are expected to engage substantively with these questions.

## AI-assisted contributions

AI-assisted development tooling is welcome. Recyclarr itself is developed with AI assistance, so
there's no stigma around using these tools.

The standard is **comprehension and accountability**: every contributor must understand and be able
to explain every change they submit, regardless of how it was produced. The project doesn't police
what tools people use. It holds all contributions to the same quality and comprehension bar. A
contributor who used AI to draft code they fully understand and can defend is treated no differently
from one who typed every character by hand.

### AI instructions

`AGENTS.md` holds the project's conventions, architecture, and coding standards. Most AI coding
tools read it, and it is also a quick introduction for human contributors. `.opencode/` adds
optional skills for [OpenCode] users; no tool is required.

[OpenCode]: https://opencode.ai

## Development Setup

1. Install [mise] and [Docker].
1. Run `mise install` in the repository root to install the tools in `.config/mise.toml`.
1. Run `pre-commit install`. The hooks run every formatter and linter on commit.
1. Use an editor with C# and [EditorConfig] support. A [CSharpier plugin][csharpier_plugins] with
   format on save is optional; the hooks format C# code too.

## Code Standards

Rules the hooks do not enforce:

- C# code must build with zero warnings. Compiler and analyzer diagnostics are errors; [CodeQL]
  scans for security issues.
- Commits must follow [Conventional Commits][commits]. `AGENTS.md` describes how to choose the type
  and scope.

## Running Recyclarr Locally

Start the Sonarr, Radarr, and notification services defined in `compose.yaml`:

```bash
docker compose up -d
```

Run a CLI command. Without a configured server, the CLI starts a temporary server for the command:

```bash
dotnet run --project src/Recyclarr.Cli -- sync --preview
```

To work on the server itself, run it with hot reload. mise sets `RECYCLARR_API_KEY` to a fixed
development key:

```bash
dotnet watch --project src/Recyclarr.Server
```

### Testing Notifications

The Apprise service forwards notifications to the ntfy service. Add this to `settings.yml`, run a
sync, then open <http://localhost:8090/recyclarr>:

```yml
notifications:
  apprise:
    mode: stateful
    base_url: http://localhost:8000
    key: recyclarr
```

## Release Process

Recyclarr follows [Semantic Versioning][semver], using [GitVersion] to automatically version the
executable according to [Conventional Commits][commits] rules.

### Making a Release

1. Run the `Prepare Release` workflow from the GitHub Actions page using the `master` branch.
1. Review and merge the generated release pull request.

The workflow updates the changelog according to [Keep a Changelog][changelog] rules. Merging the
pull request creates the GitVersion release tag and starts the release pipeline.

If the tagged build needs an infrastructure or CI fix, push the fix to `master` and run the
`Recover Release` workflow. Recovery is only available for the newest tag when it has no GitHub
release and Unreleased remains empty.

### Automated Release Process

The GitHub workflows handle:

1. Compiling .NET projects
1. Creating a [Github Release][release] with artifacts
1. Building and publishing Docker images to [Github Container Registry][ghcr] and
   [Docker Hub][dockerhub]
1. Sending notifications to the `#related-announcements` channel in the
   [TRaSH Guides Discord][discord]

## Additional Development Tasks

### Updating `.gitignore`

Execute `scripts/Update-Gitignore.ps1` from the repo root. This pulls the latest relevant
`.gitignore` patterns from [gitignore.io](https://gitignore.io) and commits them to your current
branch.

### Testing Discord Notifier

1. Make a GET request to `https://api.github.com/repos/recyclarr/recyclarr/releases/tags/v8.0.0` (or
   any version)

1. Save the response JSON to `ci/notify/release.json`

1. In the `ci/notify` directory, run:

   ```bash
   jq -r '.assets[].browser_download_url' release.json > assets.txt
   jq -r '.body' release.json > changelog.txt
   ```

1. Run the notifier with your test Discord webhook:

   ```bash
   python ./discord_notify.py \
     --version v8.0.0 \
     --repo recyclarr/recyclarr \
     --webhook-url https://discord.com/api/webhooks/your_webhook_url \
     --changelog ./changelog.txt \
     --assets ./assets.txt
   ```

[mise]: https://mise.jdx.dev/
[Docker]: https://docs.docker.com/get-started/get-docker/
[CodeQL]: https://codeql.github.com/
[EditorConfig]: https://editorconfig.org/
[csharpier_plugins]: https://csharpier.com/docs/Editors
[commits]: https://www.conventionalcommits.org/en/v1.0.0/
[semver]: https://semver.org/
[GitVersion]: https://gitversion.net/
[changelog]: https://keepachangelog.com/en/1.0.0/
[release]: https://github.com/recyclarr/recyclarr/releases
[ghcr]: https://github.com/recyclarr/recyclarr/pkgs/container/recyclarr
[discord]: https://discord.com/invite/Vau8dZ3
[dockerhub]: https://hub.docker.com/r/recyclarr/recyclarr
