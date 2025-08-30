using Recyclarr.Cli.Console;
using Recyclarr.Cli.Tests;
using Recyclarr.TestLibrary;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CliCommandIntegrationTest : CliIntegrationFixture
{
    [Test]
    public async Task List_custom_format_radarr_score_sets()
    {
        // StubRepoUpdater provides hierarchical structure: repositories/trash-guides/default
        var reposDir = Fs.CurrentDirectory().SubDirectory("repositories").SubDirectory("trash-guides").SubDirectory("default");
        var targetDir = reposDir.SubDirectory("docs").SubDirectory("json").SubDirectory("radarr").SubDirectory("cf");
        Fs.AddFilesFromEmbeddedNamespace(
            targetDir,
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
        // StubRepoUpdater provides hierarchical structure: repositories/trash-guides/default
        var reposDir = Fs.CurrentDirectory().SubDirectory("repositories").SubDirectory("trash-guides").SubDirectory("default");
        var targetDir = reposDir.SubDirectory("docs").SubDirectory("json").SubDirectory("sonarr").SubDirectory("cf");
        Fs.AddFilesFromEmbeddedNamespace(
            targetDir,
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
        // StubRepoUpdater provides hierarchical structure: repositories/trash-guides/default
        var reposDir = Fs.CurrentDirectory().SubDirectory("repositories").SubDirectory("trash-guides").SubDirectory("default");
        var targetDir = reposDir.SubDirectory("docs").SubDirectory("json").SubDirectory("sonarr").SubDirectory("naming");
        Fs.AddFilesFromEmbeddedNamespace(
            targetDir,
            typeof(CliCommandIntegrationTest),
            "Data/sonarr/naming"
        );

        var exitCode = await CliSetup.Run(Container, ["list", "naming", "sonarr"]);

        exitCode.Should().Be(0);
        Console.Output.Should().ContainAll("default", "plex-imdb", "original");
    }
}
