using System.IO.Abstractions;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ConfigTemplates;
using Recyclarr.ResourceProviders.Git;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class ConfigCreationProcessorIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Config_file_created_when_using_default_path()
    {
        _ = Resolve<IConfigTemplatesResourceQuery>(); // Ensure resource provider is initialized
        // Create a mock templates.json file in the expected location
        var mockRepoPath = "/repos/config-templates-default";
        Fs.AddDirectory(mockRepoPath);
        Fs.AddFile($"{mockRepoPath}/templates.json", new MockFileData("{}"));

        var sut = Resolve<ConfigCreationProcessor>();

        sut.Process(new ConfigCreateCommand.CliSettings { Path = null });

        var file = Fs.GetFile(Paths.AppDataDirectory.File("recyclarr.yml"));
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Config_file_created_when_using_user_specified_path()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = Fs.CurrentDirectory()
                .SubDirectory("user")
                .SubDirectory("specified")
                .File("file.yml")
                .FullName,
        };

        sut.Process(settings);

        var file = Fs.GetFile(settings.Path);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Should_throw_if_file_already_exists()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = Fs.CurrentDirectory().File("file.yml").FullName,
        };

        Fs.AddEmptyFile(settings.Path);

        var act = () => sut.Process(settings);

        act.Should().Throw<FileExistsException>();
    }

    [Test]
    public async Task Template_id_matching_works()
    {
        const string templatesJson = """
            {
              "radarr": [
                {
                  "template": "template-file1.yml",
                  "id": "template1"
                }
              ],
              "sonarr": [
                {
                  "template": "template-file2.yml",
                  "id": "template2"
                },
                {
                  "template": "template-file3.yml",
                  "id": "template3"
                }
              ]
            }
            """;

        // Initialize the Git repository service to populate repository paths
        var gitRepositoryService = Resolve<IGitRepositoryService>();
        await gitRepositoryService.InitializeAsync(null, CancellationToken.None);
        // Create a mock templates.json file in the expected location
        var mockRepoPath = Paths
            .ReposDirectory.SubDirectory("config-templates")
            .SubDirectory("official");
        Fs.AddDirectory(mockRepoPath);
        Fs.AddFile(mockRepoPath.File("templates.json"), new MockFileData(templatesJson));
        Fs.AddEmptyFile(mockRepoPath.File("template-file1.yml"));
        Fs.AddEmptyFile(mockRepoPath.File("template-file2.yml"));
        // This one shouldn't show up in the result because the user didn't ask for it
        Fs.AddEmptyFile(mockRepoPath.File("template-file3.yml"));

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns(
            [
                "template1",
                "template2",
                // This one shouldn't show up in the results because:
                // User specified it, but no template file exists for it.
                "template4",
            ]
        );

        var sut = Resolve<ConfigCreationProcessor>();
        sut.Process(settings);

        Fs.AllFiles.Should()
            .Contain(
                [
                    Paths.ConfigsDirectory.File("template-file1.yml").FullName,
                    Paths.ConfigsDirectory.File("template-file2.yml").FullName,
                ]
            );
    }
}
