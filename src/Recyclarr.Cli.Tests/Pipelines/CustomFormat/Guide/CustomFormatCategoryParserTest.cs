using System.IO.Abstractions;
using Recyclarr.Cli.Pipelines.CustomFormat.Guide;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatCategoryParserTest
{
    [Test, AutoMockData]
    public void It_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CustomFormatCategoryParser sut)
    {
        const string markdown = @"
## INDEX

------

| Audio Advanced #1                         | Audio Advanced #2               | Anime                                                                 | Anime       |
| ----------------------------------------- | ------------------------------- | --------------------------------------------------------------------- | ----------- |
| [TrueHD ATMOS](#truehd-atmos)             | [FLAC](#flac)                   | [Anime Web Tier 01 (Muxers)](#anime-web-tier-01-muxers)               | [v0](#v0)   |
| [DTS X](#dts-x)                           | [PCM](#pcm)                     | [Anime Web Tier 02 (Top FanSubs)](#anime-web-tier-02-top-fansubs)     | [v1](#v1)   |
| [ATMOS (undefined)](#atmos-undefined)     | [DTS-HD HRA](#dts-hd-hra)       | [Anime Web Tier 03 (Official Subs)](#anime-web-tier-03-official-subs) | [v2](#v2)   |
| [DD+ ATMOS](#dd-atmos)                    | [AAC](#aac)                     | [Anime Web Tier 04 (Official Subs)](#anime-web-tier-04-official-subs) | [v3](#v3)   |
| [TrueHD](#truehd)                         | [DD](#dd)                       | [Anime Web Tier 05 (FanSubs)](#anime-web-tier-05-fansubs)             | [v4](#v4)   |
| [DTS-HD MA](#dts-hd-ma)                   | [MP3](#mp3)                     | [Anime Web Tier 06 (FanSubs)](#anime-web-tier-06-fansubs)             | [VRV](#vrv) |
| [DD+](#ddplus)                            | [Opus](#opus)                   | [Anime Raws](#anime-raws)                                             |             |
| [DTS-ES](#dts-es)                         |                                 | [Anime LQ Groups](#anime-lq-groups)                                   |             |
| [DTS](#dts)                               |                                 |                                                                       |             |
|                                           |                                 |                                                                       |             |

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

        var result = sut.Parse(file);

        result.Select(x => x.CategoryName).Distinct()
            .Should().BeEquivalentTo(
                "Anime",
                "Audio Advanced #1",
                "Audio Advanced #2",
                "Movie Versions",
                "Unwanted"
            );

        result.Where(x => x.CategoryName == "Audio Advanced #1").Select(x => (x.CfName, x.CfAnchor))
            .Should().BeEquivalentTo(new[]
            {
                ("TrueHD ATMOS", "truehd-atmos"),
                ("DTS X", "dts-x"),
                ("ATMOS (undefined)", "atmos-undefined"),
                ("DD+ ATMOS", "dd-atmos"),
                ("TrueHD", "truehd"),
                ("DTS-HD MA", "dts-hd-ma"),
                ("DD+", "ddplus"),
                ("DTS-ES", "dts-es"),
                ("DTS", "dts")
            });

        result.Where(x => x.CategoryName == "Anime").Select(x => (x.CfName, x.CfAnchor))
            .Should().BeEquivalentTo(new[]
            {
                ("Anime Web Tier 01 (Muxers)", "anime-web-tier-01-muxers"),
                ("Anime Web Tier 02 (Top FanSubs)", "anime-web-tier-02-top-fansubs"),
                ("Anime Web Tier 03 (Official Subs)", "anime-web-tier-03-official-subs"),
                ("Anime Web Tier 04 (Official Subs)", "anime-web-tier-04-official-subs"),
                ("Anime Web Tier 05 (FanSubs)", "anime-web-tier-05-fansubs"),
                ("Anime Web Tier 06 (FanSubs)", "anime-web-tier-06-fansubs"),
                ("Anime Raws", "anime-raws"),
                ("Anime LQ Groups", "anime-lq-groups"),
                ("v0", "v0"),
                ("v1", "v1"),
                ("v2", "v2"),
                ("v3", "v3"),
                ("v4", "v4"),
                ("VRV", "vrv")
            });
    }
}
