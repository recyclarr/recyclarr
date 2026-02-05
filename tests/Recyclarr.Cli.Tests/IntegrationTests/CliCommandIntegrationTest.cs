using System.IO.Abstractions;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CliCommandIntegrationTest : CliIntegrationFixture
{
    [Test]
    public async Task List_naming_sonarr()
    {
        // Add naming data file at the expected path (StubRepoUpdater handles metadata.json)
        var officialRepoPath = Paths
            .ResourceDirectory.SubDirectory("trash-guides")
            .SubDirectory("git")
            .SubDirectory("official");

        var namingFile = officialRepoPath
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("sonarr")
            .SubDirectory("naming")
            .File("sonarr-naming.json");

        Fs.AddFile(
            namingFile,
            new MockFileData(
                """
                {
                  "season": { "default": "Season {season:00}" },
                  "series": {
                    "default": "{Series TitleYear}",
                    "plex-imdb": "{Series TitleYear} {imdb-{ImdbId}}"
                  },
                  "episodes": {
                    "standard": {
                      "default": "{Series TitleYear} - S{season:00}E{episode:00}",
                      "original": "{Original Title}"
                    },
                    "daily": {
                      "default": "{Series TitleYear} - {Air-Date}",
                      "original": "{Original Title}"
                    },
                    "anime": {
                      "default": "{Series TitleYear} - S{season:00}E{episode:00}"
                    }
                  }
                }
                """
            )
        );

        var exitCode = await CliSetup.Run(Container, ["list", "naming", "sonarr"]);

        exitCode.Should().Be(0);
    }
}
