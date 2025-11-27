using System.IO.Abstractions;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.TestLibrary;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CliCommandIntegrationTest : CliIntegrationFixture
{
    [Test]
    public async Task List_custom_format_radarr_score_sets()
    {
        var reposDir = Paths
            .ReposDirectory.SubDirectory("trash-guides")
            .SubDirectory("git")
            .SubDirectory("official");

        var targetDir = reposDir
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("radarr")
            .SubDirectory("cf");
        Fs.AddFilesFromEmbeddedNamespace(
            targetDir,
            typeof(CliCommandIntegrationTest),
            "Data.radarr.cfs"
        );

        var exitCode = await CliSetup.Run(
            Container,
            ["list", "custom-formats", "radarr", "--score-sets"]
        );

        exitCode.Should().Be(0);
        Console.Output.Should().ContainAll("default", "sqp-1-1080p", "sqp-1-2160p");
    }

    [Test]
    public async Task List_custom_format_sonarr_score_sets()
    {
        var reposDir = Paths
            .ReposDirectory.SubDirectory("trash-guides")
            .SubDirectory("git")
            .SubDirectory("official");

        var targetDir = reposDir
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("sonarr")
            .SubDirectory("cf");
        Fs.AddFilesFromEmbeddedNamespace(
            targetDir,
            typeof(CliCommandIntegrationTest),
            "Data.sonarr.cfs"
        );

        var exitCode = await CliSetup.Run(
            Container,
            ["list", "custom-formats", "sonarr", "--score-sets"]
        );

        exitCode.Should().Be(0);
        Console.Output.Should().ContainAll("default", "anime-sonarr", "french-multi");
    }

    [Test]
    public async Task List_custom_format_score_sets_fails_without_service_type()
    {
        var act = () => CliSetup.Run(Container, ["list", "custom-formats", "--score-sets"]);
        await act.Should().ThrowAsync<CommandRuntimeException>();
    }

    [Test]
    public async Task List_naming_sonarr()
    {
        // Add naming data file at the expected path (StubRepoUpdater handles metadata.json)
        var officialRepoPath = Paths
            .ReposDirectory.SubDirectory("trash-guides")
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
        Console.Output.Should().ContainAll("default", "plex-imdb", "original");
    }
}
