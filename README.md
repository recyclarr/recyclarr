# Recyclarr

[![GitHub License](https://img.shields.io/github/license/recyclarr/recyclarr)](https://github.com/recyclarr/recyclarr/blob/master/LICENSE)
[![Build Workflow Status](https://img.shields.io/github/actions/workflow/status/recyclarr/recyclarr/build.yml?branch=master&logo=githubactions)](https://github.com/recyclarr/recyclarr/actions/workflows/build.yml?query=branch%3Amaster)
[![CodeQL](https://github.com/recyclarr/recyclarr/actions/workflows/codeql.yml/badge.svg)](https://github.com/recyclarr/recyclarr/actions/workflows/codeql.yml)
[![GitHub Release](https://img.shields.io/github/v/release/recyclarr/recyclarr?logo=github)](https://github.com/recyclarr/recyclarr/releases/)
[![Discord](https://img.shields.io/discord/492590071455940612?label=TRaSH-Guides&logo=discord)][discord]

A command-line application that will automatically synchronize recommended settings from the
[TRaSH guides](https://trash-guides.info/) to your Sonarr/Radarr instances.

## Features

Recyclarr supports Radarr and Sonarr (Sonarr v4 or newer). The following information can be synced
to these services from the TRaSH Guides. For a more detailed features list, see the [Features] page.

[Features]: https://recyclarr.dev/guide/features/

- Quality Profiles, including qualities and quality groups
- Custom Formats, individually or as groups, including scores (from guide or manual)
- Quality Definitions (file sizes)
- Media Naming Formats
- Media Management (Propers/Repacks)

## Getting Started

New to Recyclarr? The [Getting Started] guide walks through installing it with Docker and syncing
your first TRaSH Guides quality profile from a ready-made template.

[Getting Started]: https://recyclarr.dev/guide/getting-started/

> [!WARNING]
> The `latest` Docker tag is not available on Docker Hub or GHCR. Use a major version tag (e.g. `8`)
> instead. See the [Docker tags](https://recyclarr.dev/guide/installation/docker/#tags) docs.

## Read the Documentation

[![view - Documentation](https://img.shields.io/badge/view-Documentation-blue?style=for-the-badge)](https://recyclarr.dev/)

Main documentation is located at [recyclarr.dev](https://recyclarr.dev/). Links provided below for
some main topics.

- [Installation](https://recyclarr.dev/guide/installation/)
- [Command Line Reference](https://recyclarr.dev/cli/)
- [Configuration Reference](https://recyclarr.dev/reference/configuration/)
- [Settings Reference](https://recyclarr.dev/reference/settings/)
- [Errors & Warnings](https://recyclarr.dev/guide/troubleshooting/errors/)
- [Upgrade Guides](https://recyclarr.dev/guide/upgrade-guide/)

## Getting Support

For help with using Recyclarr, please join the [TRaSH-Guides Discord][discord] and ask in the
`#recyclarr` channel. Read [Getting Help](https://recyclarr.dev/guide/troubleshooting/help/) first
for the details to include with your question.

> [!IMPORTANT]
> The GitHub Issues section is reserved for:
>
> - Bug reports
> - Feature requests
>
> Please do not use GitHub Issues for general support questions or configuration help. Report
> security vulnerabilities privately as described in the [Security Policy](.github/SECURITY.md).

## Contributing

Contributions are welcome. Non-trivial changes need an approved design in an issue before a pull
request is opened. See [CONTRIBUTING.md](CONTRIBUTING.md) for the workflow, standards, and
development setup.

## Sponsors

Thank you to all who have supported Recyclarr!

<!-- markdownlint-disable MD033 MD013 -->
<!-- sponsors --><a href="https://github.com/mvanbaak"><img src="https:&#x2F;&#x2F;github.com&#x2F;mvanbaak.png" width="60px" alt="User avatar: Michiel van Baak Jansen" /></a><a href="https://github.com/tiemonl"><img src="https:&#x2F;&#x2F;github.com&#x2F;tiemonl.png" width="60px" alt="User avatar: Liam Tiemon" /></a><a href="https://github.com/yammes08"><img src="https:&#x2F;&#x2F;github.com&#x2F;yammes08.png" width="60px" alt="User avatar: yammes08" /></a><a href="https://github.com/fabricionaweb"><img src="https:&#x2F;&#x2F;github.com&#x2F;fabricionaweb.png" width="60px" alt="User avatar: Fabricio" /></a><a href="https://github.com/jporto24"><img src="https:&#x2F;&#x2F;github.com&#x2F;jporto24.png" width="60px" alt="User avatar: jporto24" /></a><a href="https://github.com/daithi-coyle"><img src="https:&#x2F;&#x2F;github.com&#x2F;daithi-coyle.png" width="60px" alt="User avatar: daithi-coyle" /></a><a href="https://github.com/buroa"><img src="https:&#x2F;&#x2F;github.com&#x2F;buroa.png" width="60px" alt="User avatar: Steven Kreitzer" /></a><a href="https://github.com/BaukeZwart"><img src="https:&#x2F;&#x2F;github.com&#x2F;BaukeZwart.png" width="60px" alt="User avatar: Bauke" /></a><a href="https://github.com/kitizz"><img src="https:&#x2F;&#x2F;github.com&#x2F;kitizz.png" width="60px" alt="User avatar: Kit Ham" /></a><a href="https://github.com/svikartrok"><img src="https:&#x2F;&#x2F;github.com&#x2F;svikartrok.png" width="60px" alt="User avatar: Rok Švikart" /></a><a href="https://github.com/pedorich-n"><img src="https:&#x2F;&#x2F;github.com&#x2F;pedorich-n.png" width="60px" alt="User avatar: Nikita Pedorich" /></a><a href="https://github.com/bmikaili"><img src="https:&#x2F;&#x2F;github.com&#x2F;bmikaili.png" width="60px" alt="User avatar: Berzan" /></a><!-- sponsors -->
<!-- markdownlint-enable MD033 MD013 -->

## Powered By

<!-- markdownlint-disable MD033 MD013 -->

<a href="https://www.jetbrains.com/rider/" target="_blank"><img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider_icon.svg" alt="Jetbrains Rider" style="margin-right: 10px;"></a>

<!-- markdownlint-enable MD033 MD013 -->

[discord]: https://discord.com/invite/Vau8dZ3
