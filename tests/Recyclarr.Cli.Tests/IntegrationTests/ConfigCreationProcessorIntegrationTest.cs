using System.IO.Abstractions;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Cli.Tests.IntegrationTests;

[CliDataSource]
internal sealed class ConfigCreationProcessorIntegrationTest(
    IConfigCreationProcessor sut,
    ProviderInitializationFactory providerFactory,
    MockFileSystem fs,
    IAppPaths paths
)
{
    [Test]
    public void Config_file_created_when_using_default_path()
    {
        sut.Process(new ConfigCreateCommand.CliSettings { Path = null });

        var file = fs.GetFile(paths.ConfigDirectory.File("recyclarr.yml"));
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Config_file_created_when_using_user_specified_path()
    {
        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = fs.CurrentDirectory()
                .SubDirectory("user")
                .SubDirectory("specified")
                .File("file.yml")
                .FullName,
        };

        sut.Process(settings);

        var file = fs.GetFile(settings.Path);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Should_throw_if_file_already_exists()
    {
        var settings = new ConfigCreateCommand.CliSettings
        {
            Path = fs.CurrentDirectory().File("file.yml").FullName,
        };

        fs.AddEmptyFile(settings.Path);

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
                  "id": "template-file1"
                }
              ],
              "sonarr": [
                {
                  "template": "template-file2.yml",
                  "id": "template-file2"
                },
                {
                  "template": "template-file3.yml",
                  "id": "template-file3"
                }
              ]
            }
            """;

        // Create a mock templates.json file in the expected location BEFORE initialization
        var mockRepoPath = paths
            .ResourceDirectory.SubDirectory("config-templates")
            .SubDirectory("git")
            .SubDirectory("official");
        fs.AddDirectory(mockRepoPath);
        fs.AddFile(mockRepoPath.File("templates.json"), new MockFileData(templatesJson));
        fs.AddEmptyFile(mockRepoPath.File("template-file1.yml"));
        fs.AddEmptyFile(mockRepoPath.File("template-file2.yml"));
        // This one shouldn't show up in the result because the user didn't ask for it
        fs.AddEmptyFile(mockRepoPath.File("template-file3.yml"));

        // Initialize resource providers to populate repository paths
        await providerFactory.InitializeProvidersAsync(null, CancellationToken.None);

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns([
            "template-file1",
            "template-file2",
            // This one shouldn't show up in the results because:
            // User specified it, but no template file exists for it.
            "template-file4",
        ]);

        sut.Process(settings);

        fs.AllFiles.Should()
            .Contain([
                paths.YamlConfigDirectory.File("template-file1.yml").FullName,
                paths.YamlConfigDirectory.File("template-file2.yml").FullName,
            ]);
    }
}
