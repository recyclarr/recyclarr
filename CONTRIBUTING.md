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

The project's `Dockerfile` build requires the Recyclarr build output to be placed in a specific
location in order to succeed. The location is below, relative to the clone root:

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
