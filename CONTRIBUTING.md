# Contributing

First, thank you for your interest in contributing to my project. Below is a list of requirements
that everyone should follow.

1. To avoid wasting your time and effort, please ensure all ideas get discussed first. Either visit
   [the Ideas discussion board][ideas] and open a thread there, or create a new issue. I ask that
   you do this to avoid the potential of rejecting work already done in a pull request.

1. **For Markdown changes,** Any and all changes must pass configured [markdownlint] rules (see the
   `.markdownlint.json` files in this repository for project-specific adjustments to those rules).

1. **For C# changes,** code must conform to the project's style. My day to day coding is done in
   Jetbrains Rider. If using that IDE, doing a simple [Code Cleanup] on modified source files should
   be enough. If you're using Visual Studio or some other editor, you are on your own. Formatting
   rules are stored in `src/.editorconfig` and `src/TrashUpdater.sln.DotSettings`.

[ideas]: https://github.com/recyclarr/recyclarr/discussions/categories/ideas
[markdownlint]: https://github.com/DavidAnson/markdownlint
[Code Cleanup]: https://www.jetbrains.com/help/rider/Code_Cleanup__Index.html

## Docker Development

The project's `Dockerfile` builds in two different mods: Development and production mode.

### Production Build

This is the default build type for the image. Given a specific version number, it will grab the
appropriate binary from the corresponding Github Release and install that into the image.

### Development Build

This build allows you to make changes to Recyclarr and pull those into a local docker image build.
This is especially useful if you want to test changes in Recyclarr before it is released, since the
production mode of Recyclarr requires a Github release to pull from.

To enable development builds, specify the build argument `BUILD_FROM_BRANCH`. The workflow I use
goes something like this:

1. Create a branch to work out of: `git checkout -b docker origin/master`.
1. Make some C# code changes, commit, and **push to the remote repo**.
1. Build the docker image locally:

   ```sh
   docker compose build --no-cache --progress plain --build-arg BUILD_FROM_BRANCH=docker
   ```

1. Execute it locally:

   ```sh
   docker compose run --rm recyclarr sonarr
   ```

### Build Arguments

- `RELEASE_TAG` (Default: `latest`)<br>
  The git tag (e.g. `v2.1.2`) that represents the Github Release in the upstream repository to grab
  binaries from. May also use `latest` to represent the latest Github Release. Only used in
  Production builds.

- `TARGETPLATFORM` (Default: empty)<br>
  Required. Specifies the runtime architecture of the image and is used to pull the correct prebuilt
  binary from the specified Github Release. See the table in the Platform Support section for a list
  of valid values.

- `REPOSITORY` (Default: `recyclarr/recyclarr`)<br>
  The Github repository name (either `user/repo` or `organization/repo` format) used to grab the
  prebuilt release from (in Production builds) or to clone (in Development builds).

- `BUILD_FROM_BRANCH` (Default: empty)<br>
  If specified, Development build mode is enabled and the branch name specified here is used to
  compile Recyclarr and use its final binary in the resulting docker image.

### Platform Support

| Docker Platform | Recyclarr Runtime  |
| --------------- | ------------------ |
| `linux/arm/v7`  | `linux-musl-arm`   |
| `linux/arm64`   | `linux-musl-arm64` |
| `linux/amd64`   | `linux-musl-x64`   |
