# Contributing

First, thank you for your interest in contributing to my project. Below is a list of requirements
that everyone should follow.

1. To avoid wasting your time and effort, please ensure all ideas get discussed first. Either visit
   [the Ideas discussion board][ideas] and open a thread there. I ask that you do this to avoid the
   potential of rejecting work already done in a pull request.

1. **For Markdown changes,** any and all changes must pass configured [markdownlint] rules (see the
   `.markdownlint.json` files in this repository for project-specific adjustments to those rules).

1. **For C# changes,** code must conform to the project's style. My day to day coding is done in
   Jetbrains Rider. If using that IDE, doing a simple [Code Cleanup] on modified source files should
   be enough. Make sure to select the "Recyclarr Cleanup" profile when you do the code cleanup. If
   you're using Visual Studio or some other editor, you are on your own. Formatting rules are stored
   in `src/.editorconfig` and `src/Recyclarr.sln.DotSettings`.

[ideas]: https://github.com/recyclarr/recyclarr/discussions/categories/ideas
[markdownlint]: https://github.com/DavidAnson/markdownlint
[Code Cleanup]: https://www.jetbrains.com/help/rider/Code_Cleanup__Index.html

## Tooling Requirements

The following tools are required:

- .NET SDK 7.0 and tooling (e.g. `dotnet`)
- Powershell v5.1 or greater
- Docker CLI (Docker Desktop on Windows)

The following tools are *highly recommended* but not strictly required:

- Jetbrains Rider (IDE for editing C# code)
- Visual Studio Code (install workspace-recommended extensions as well)

Other required tooling can be installed via the `Install-Tooling.ps1` powershell script. It's also a
good idea to occasionally run this for upgrade purposes, too.

## Docker Development

The project's `Dockerfile` build requires the Recyclarr build output to be placed in a specific
location in order to succeed. The location is below, relative to the repository root:

```txt
docker/artifacts/recyclarr-${{runtime}}
```

Where `${{runtime}}` is one of the runtimes compatible with `dotnet publish`, such as
`linux-musl-x64`.

There is a convenience script named `docker/Build-Artifacts.ps1` that will perform a build and place
the output in the appropriate location for you. This simplifies the process of testing docker
locally to these steps:

1. Run the convenience script to build and publish Recyclarr to the Docker artifacts directory:

   ```sh
   pwsh ci/Build-Artifacts.ps1
   ```

   > *Note:* The runtime defaults to `linux-musl-x64` but you can pass in an override as the first
   > placeholder argument to the above command.

1. Execute a Docker build locally via compose:

   ```sh
   docker compose build --no-cache --progress plain
   ```

1. Run the container to test it:

   ```sh
   docker compose run --rm recyclarr sonarr
   ```

### Build Arguments

- `TARGETPLATFORM` (Default: empty)<br>
  Required. Specifies the runtime architecture of the image and is used to pull the correct prebuilt
  binary from the specified Github Release. See the table in the Platform Support section for a list
  of valid values.

### Platform Support

| Docker Platform | Recyclarr Runtime  |
| --------------- | ------------------ |
| `linux/arm/v7`  | `linux-musl-arm`   |
| `linux/arm64`   | `linux-musl-arm64` |
| `linux/amd64`   | `linux-musl-x64`   |

## Conventional Commits

This project uses and enforces a variation of [Conventional Commits][commits]. The below official
commit types are used:

Official:

- `build`: Update project files, settings, etc.
- `chore`: Anything not code related or that falls into other categories.
- `ci`: Changes to CI/CD scripts or configuration.
- `docs`: Updates to non-code documentation (markdown, readme, etc).
- `feat`: A new feature was implemented.
- `fix`: A defect or security issue was fixed.
- `perf`: Change in code related to improving performance.
- `refactor`: A code change that does not impact the observable functionality or shape of the apps.
- `revert`: Prefix to be used for commits made by the `git revert` command.
- `style`: A whitespace or code cleanup change in code.
- `test`: Updates to unit test code only.

Specialized:

- `change`: Change to existing functionality.
- `deprecate`: Deprecation of existing functionality.
- `remove`: Removal of existing feature.

## Release Process

Release numbering follows [Semantic Versioning][semver]. The [GitVersion] package is used in .NET
projects to automatically version the executable according to [Conventional Commits][commits] rules
in conjunction with semantic versioning.

The goal is to allow commit messages to drive the way the semantic version number is advanced during
development. When a feature is implemented, the corresponding commit results in the minor version
number component being advanced by 1. Similarly, the patch portion is advanced by 1 when a bugfix is
committed.

To make a release, follow these steps:

1. Run `Prepare-Release.ps1`. This will do the following:
   1. Update the changelog for the release according to [Keep a Changelog][changelog] rules.
   1. Commit the changelog updates.
   1. Create a tag for the release (using GitVersion).
1. Use Git to push the new tag and commits on `master` upstream where the Github Workflows will take
   over.

The Github Workflows manage the release process after the push by doing the following:

1. Compile the .NET projects.
1. Create a [Github Release][release] with the .NET artifacts attached.
1. Build and publish a new Docker image to the [Github Container Registry][ghcr] and [Docker
   Hub][dockerhub].
1. Send a release notification to the `#related-announcements` channel in the official [TRaSH Guides
   Discord][discord].

[semver]: https://semver.org/
[GitVersion]: https://gitversion.net/
[commits]: https://www.conventionalcommits.org/en/v1.0.0/
[changelog]: https://keepachangelog.com/en/1.0.0/
[release]: https://github.com/recyclarr/recyclarr/releases
[ghcr]: https://github.com/recyclarr/recyclarr/pkgs/container/recyclarr
[discord]: https://discord.com/invite/Vau8dZ3

## Update `.gitignore`

Execute the `Update-Gitignore.ps1` script using Powershell. The working directory *must* be the root
of the repo. This will pull the latest relevant `.gitignore` patterns from
[gitignore.io](https://gitignore.io) and commit them automatically to your current branch.
