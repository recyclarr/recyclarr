using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Platform;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Core.Tests.TrashGuide.CustomFormat;

internal sealed class CustomFormatCategoryParserTest
{
    [Test, AutoMockData]
    public void It_works(
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        CustomFormatCategoryParser sut
    )
    {
        const string markdownFilename = "Radarr-collection-of-custom-formats.md";
        var resourceReader = new ResourceDataReader(typeof(CustomFormatCategoryParserTest), "Data");
        var markdown = resourceReader.ReadData(markdownFilename);

        var file = fs.CurrentDirectory()
            .SubDirectory("docs")
            .SubDirectory("Radarr")
            .File(markdownFilename);

        fs.AddFile(file.FullName, new MockFileData(markdown));

        var result = sut.Parse(file);

        result
            .Select(x => x.CategoryName)
            .Distinct()
            .Should()
            .BeEquivalentTo(
                "Audio Advanced #1",
                "Audio Advanced #2",
                "Audio Channels",
                "HDR Formats",
                "Movie Versions",
                "Unwanted",
                "HQ Release Groups",
                "General Streaming Services",
                "Asian Streaming Services",
                "Dutch Streaming Services",
                "UK Streaming Services",
                "Misc Streaming Services",
                "Anime Streaming Services",
                "Miscellaneous",
                "French Audio Version",
                "French Source Groups",
                "Anime",
                "Anime Optional"
            );

        result
            .Where(x => x.CategoryName == "Audio Advanced #1")
            .Select(x => (x.CfName, x.CfAnchor))
            .Should()
            .BeEquivalentTo(
                [
                    ("TrueHD ATMOS", "truehd-atmos"),
                    ("DTS X", "dts-x"),
                    ("ATMOS (undefined)", "atmos-undefined"),
                    ("DD+ ATMOS", "ddplus-atmos"),
                    ("TrueHD", "truehd"),
                    ("DTS-HD MA", "dts-hd-ma"),
                    ("DD+", "ddplus"),
                    ("DTS-ES", "dts-es"),
                    ("DTS", "dts"),
                ]
            );

        result
            .Where(x => x.CategoryName == "Anime")
            .Select(x => (x.CfName, x.CfAnchor))
            .Should()
            .BeEquivalentTo(
                [
                    ("Anime BD Tier 01 (Top SeaDex Muxers)", "anime-bd-tier-01-top-seadex-muxers"),
                    ("Anime BD Tier 02 (SeaDex Muxers)", "anime-bd-tier-02-seadex-muxers"),
                    ("Anime BD Tier 03 (SeaDex Muxers)", "anime-bd-tier-03-seadex-muxers"),
                    ("Anime BD Tier 04 (SeaDex Muxers)", "anime-bd-tier-04-seadex-muxers"),
                    ("Anime BD Tier 05 (Remuxes)", "anime-bd-tier-05-remuxes"),
                    ("Anime BD Tier 06 (FanSubs)", "anime-bd-tier-06-fansubs"),
                    ("Anime BD Tier 07 (P2P/Scene)", "anime-bd-tier-07-p2pscene"),
                    ("Anime BD Tier 08 (Mini Encodes)", "anime-bd-tier-08-mini-encodes"),
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
                ]
            );

        result
            .Where(x => x.CategoryName == "Anime Optional")
            .Select(x => (x.CfName, x.CfAnchor))
            .Should()
            .BeEquivalentTo(
                [
                    ("Uncensored", "uncensored"),
                    ("10bit", "10bit"),
                    ("Anime Dual Audio", "anime-dual-audio"),
                    ("Dubs Only", "dubs-only"),
                ]
            );
    }
}
