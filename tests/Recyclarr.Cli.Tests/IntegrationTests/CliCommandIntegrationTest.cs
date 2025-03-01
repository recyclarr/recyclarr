using System.IO.Abstractions;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CliCommandIntegrationTest : CliIntegrationFixture
{
    private static readonly TrashRepoFileMapper Mapper = new();

    [OneTimeSetUp]
    public static async Task OneTimeSetup()
    {
        await Mapper.DownloadFiles(
            "metadata.json",
            "docs/Radarr/Radarr-collection-of-custom-formats.md",
            "docs/Sonarr/sonarr-collection-of-custom-formats.md"
        );
    }

    [SetUp]
    public void MapFiles()
    {
        Mapper.AddToFilesystem(Fs, Resolve<ITrashGuidesRepo>().Path);
    }

    [Test]
    public async Task List_custom_format_radarr_score_sets()
    {
        var repo = Resolve<ITrashGuidesRepo>();
        Fs.AddFilesFromEmbeddedNamespace(
            repo.Path.SubDirectory("docs/json/radarr/cf"),
            typeof(CliCommandIntegrationTest),
            "Data/radarr/cfs"
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
        var repo = Resolve<ITrashGuidesRepo>();
        Fs.AddFilesFromEmbeddedNamespace(
            repo.Path.SubDirectory("docs/json/sonarr/cf"),
            typeof(CliCommandIntegrationTest),
            "Data/sonarr/cfs"
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
        var repo = Resolve<ITrashGuidesRepo>();
        Fs.AddFilesFromEmbeddedNamespace(
            repo.Path.SubDirectory("docs/json/sonarr/naming"),
            typeof(CliCommandIntegrationTest),
            "Data/sonarr/naming"
        );

        var exitCode = await CliSetup.Run(Container, ["list", "naming", "sonarr"]);

        exitCode.Should().Be(0);
        Console.Output.Should().ContainAll("default", "plex-imdb", "original");
    }
}
