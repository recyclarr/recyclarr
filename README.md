# Recyclarr

[![GitHub License](https://img.shields.io/github/license/recyclarr/recyclarr)](https://github.com/recyclarr/recyclarr/blob/master/LICENSE)
[![Build Workflow Status](https://img.shields.io/github/actions/workflow/status/recyclarr/recyclarr/build.yml?branch=master&logo=githubactions)](https://github.com/recyclarr/recyclarr/actions/workflows/build.yml?query=branch%3Amaster)
[![Qodana](https://github.com/recyclarr/recyclarr/actions/workflows/qodana.yml/badge.svg)](https://qodana.cloud/projects/p5QRr)
[![GitHub Release](https://img.shields.io/github/v/release/recyclarr/recyclarr?logo=github)](https://github.com/recyclarr/recyclarr/releases/)
[![Discord](https://img.shields.io/discord/492590071455940612?label=TRaSH-Guides&logo=discord)][discord]

A command-line application that will automatically synchronize recommended settings from the [TRaSH
guides](https://trash-guides.info/) to your Sonarr/Radarr instances.

## Features

Recyclarr supports Radarr and Sonarr (v4 and higher only). The following information can be synced
to these services from the TRaSH Guides. For a more detailed features list, see the [Features] page.

[Features]: https://recyclarr.dev/guide/features/

- Quality Profiles, including qualities and quality groups
- Custom Formats, including scores (from guide or manual)
- Quality Definitions (file sizes)
- Media Naming Formats
- Media Management (Propers/Repacks)

> [!WARNING]
> The `latest` Docker tag is no longer published. If you are using `recyclarr/recyclarr:latest` or
> `ghcr.io/recyclarr/recyclarr:latest`, switch to a major version tag (e.g. `8`) to continue
> receiving updates.

## Read the Documentation

[![view - Documentation](https://img.shields.io/badge/view-Documentation-blue?style=for-the-badge)](https://recyclarr.dev/)

Main documentation is located at [recyclarr.dev](https://recyclarr.dev/). Links provided below for
some main topics.

- [Installation](https://recyclarr.dev/guide/installation/)
- [Command Line Reference](https://recyclarr.dev/cli/)
- [Configuration Reference](https://recyclarr.dev/reference/configuration/)
- [Settings Reference](https://recyclarr.dev/reference/settings/)
- [Troubleshooting](https://recyclarr.dev/guide/troubleshooting/)
- [Upgrade Guides](https://recyclarr.dev/guide/upgrade-guide/)

## Getting Support

For help with using Recyclarr, please join the [TRaSH-Guides Discord][discord] and ask in the
`#recyclarr` channel.

> [!IMPORTANT]
> The GitHub Issues section is reserved for:
>
> - Bug reports
> - Feature requests
>
> Please do not use GitHub Issues for general support questions or configuration help.

## Sponsors

Thank you to all who have supported Recyclarr!

<!-- markdownlint-disable MD033 MD013 -->
<!-- sponsors --><a href="https://github.com/mvanbaak"><img src="https:&#x2F;&#x2F;github.com&#x2F;mvanbaak.png" width="60px" alt="User avatar: Michiel van Baak Jansen" /></a><a href="https://github.com/tiemonl"><img src="https:&#x2F;&#x2F;github.com&#x2F;tiemonl.png" width="60px" alt="User avatar: Liam Tiemon" /></a><a href="https://github.com/yammes08"><img src="https:&#x2F;&#x2F;github.com&#x2F;yammes08.png" width="60px" alt="User avatar: " /></a><a href="https://github.com/fabricionaweb"><img src="https:&#x2F;&#x2F;github.com&#x2F;fabricionaweb.png" width="60px" alt="User avatar: Fabricio" /></a><a href="https://github.com/jporto24"><img src="https:&#x2F;&#x2F;github.com&#x2F;jporto24.png" width="60px" alt="User avatar: " /></a><a href="https://github.com/daithi-coyle"><img src="https:&#x2F;&#x2F;github.com&#x2F;daithi-coyle.png" width="60px" alt="User avatar: " /></a><a href="https://github.com/buroa"><img src="https:&#x2F;&#x2F;github.com&#x2F;buroa.png" width="60px" alt="User avatar: Steven Kreitzer" /></a><a href="https://github.com/BaukeZwart"><img src="https:&#x2F;&#x2F;github.com&#x2F;BaukeZwart.png" width="60px" alt="User avatar: Bauke" /></a><a href="https://github.com/kitizz"><img src="https:&#x2F;&#x2F;github.com&#x2F;kitizz.png" width="60px" alt="User avatar: Kit Ham" /></a><a href="https://github.com/svikartrok"><img src="https:&#x2F;&#x2F;github.com&#x2F;svikartrok.png" width="60px" alt="User avatar: Rok Švikart" /></a><a href="https://github.com/pedorich-n"><img src="https:&#x2F;&#x2F;github.com&#x2F;pedorich-n.png" width="60px" alt="User avatar: Nikita Pedorich" /></a><a href="https://github.com/blixten85"><img src="https:&#x2F;&#x2F;github.com&#x2F;blixten85.png" width="60px" alt="User avatar: Anders Eriksson" /></a><!-- sponsors -->
<!-- markdownlint-enable MD033 MD013 -->

## Powered By

<!-- markdownlint-disable MD033 MD013 -->

<a href="https://www.jetbrains.com/rider/" target="_blank"><img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider_icon.svg" alt="Jetbrains Rider" style="margin-right: 10px;"></a>
<a href="https://www.jetbrains.com/qodana/" target="_blank"><img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Qodana_icon.svg" alt="Jetbrains Qodana"></a>

<!-- markdownlint-enable MD033 MD013 -->

[discord]: https://discord.com/invite/Vau8dZ3
