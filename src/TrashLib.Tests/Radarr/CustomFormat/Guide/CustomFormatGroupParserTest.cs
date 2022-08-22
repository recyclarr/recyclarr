using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Services.Radarr.CustomFormat.Guide;
using TrashLib.Startup;

namespace TrashLib.Tests.Radarr.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatGroupParserTest
{
    [Test, AutoMockData]
    public void It_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CustomFormatGroupParser sut)
    {
        const string markdown = @"
## INDEX

------

| Audio Advanced #1                         | Audio Advanced #2               |
| ----------------------------------------- | ------------------------------- |
| [TrueHD ATMOS](#truehd-atmos)             | [FLAC](#flac)                   |
| [DTS X](#dts-x)                           | [PCM](#pcm)                     |
| [ATMOS (undefined)](#atmos-undefined)     | [DTS-HD HRA](#dts-hd-hra)       |
| [DD+ ATMOS](#dd-atmos)                    | [AAC](#aac)                     |
| [TrueHD](#truehd)                         | [DD](#dd)                       |
| [DTS-HD MA](#dts-hd-ma)                   | [MP3](#mp3)                     |
| [DD+](#ddplus)                            | [Opus](#opus)                   |
| [DTS-ES](#dts-es)                         |                                 |
| [DTS](#dts)                               |                                 |
|                                           |                                 |

------

| Movie Versions                                | Unwanted                           |
| --------------------------------------------- | ---------------------------------- |
| [Hybrid](#hybrid)                             | [BR-DISK](#br-disk)                |
| [Remaster](#remaster)                         | [EVO (no WEBDL)](#evo-no-webdl)    |
| [4K Remaster](#4k-remaster)                   | [LQ](#lq)                          |
| [Special Editions](#special-edition)          | [x265 (720/1080p)](#x265-7201080p) |
| [Criterion Collection](#criterion-collection) | [3D](#3d)                          |
| [Theatrical Cut](#theatrical-cut)             | [No-RlsGroup](#no-rlsgroup)        |
| [IMAX](#imax)                                 | [Obfuscated](#obfuscated)          |
| [IMAX Enhanced](#imax-enhanced)               | [DV (WEBDL)](#dv-webdl)            |
|                                               |                                    |

------
";

        var file = paths.RepoDirectory
            .SubDirectory("docs")
            .SubDirectory("Radarr")
            .File("Radarr-collection-of-custom-formats.md");

        fs.AddFile(file.FullName, new MockFileData(markdown));

        var result = sut.Parse();

        result.Keys.Should().BeEquivalentTo(
            "Audio Advanced #1",
            "Audio Advanced #2",
            "Movie Versions",
            "Unwanted"
        );

        result.Should().ContainKey("Audio Advanced #1")
            .WhoseValue.Should().BeEquivalentTo(new[]
            {
                new CustomFormatGroupItem("TrueHD ATMOS", "truehd-atmos"),
                new CustomFormatGroupItem("DTS X", "dts-x"),
                new CustomFormatGroupItem("ATMOS (undefined)", "atmos-undefined"),
                new CustomFormatGroupItem("DD+ ATMOS", "dd-atmos"),
                new CustomFormatGroupItem("TrueHD", "truehd"),
                new CustomFormatGroupItem("DTS-HD MA", "dts-hd-ma"),
                new CustomFormatGroupItem("DD+", "ddplus"),
                new CustomFormatGroupItem("DTS-ES", "dts-es"),
                new CustomFormatGroupItem("DTS", "dts")
            });
    }
}
