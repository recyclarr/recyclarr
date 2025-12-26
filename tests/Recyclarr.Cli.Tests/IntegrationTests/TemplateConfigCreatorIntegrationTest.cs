using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class TemplateConfigCreatorIntegrationTest : CliIntegrationFixture
{
    private IDirectoryInfo SetupTemplateRepo(string templatesJson)
    {
        var mockRepoPath = Paths
            .ReposDirectory.SubDirectory("config-templates")
            .SubDirectory("git")
            .SubDirectory("official");
        Fs.AddDirectory(mockRepoPath);
        Fs.AddFile(mockRepoPath.File("templates.json"), new MockFileData(templatesJson));
        return mockRepoPath;
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

        var mockRepoPath = SetupTemplateRepo(templatesJson);
        Fs.AddEmptyFile(mockRepoPath.File("template-file1.yml"));
        Fs.AddEmptyFile(mockRepoPath.File("template-file2.yml"));
        // This one shouldn't show up in the result because the user didn't ask for it
        Fs.AddEmptyFile(mockRepoPath.File("template-file3.yml"));

        // Initialize resource providers to populate repository paths
        var factory = Resolve<ProviderInitializationFactory>();
        await factory.InitializeProvidersAsync(null, CancellationToken.None);

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns([
            "template-file1",
            "template-file2",
            // This one shouldn't show up in the results because:
            // User specified it, but no template file exists for it.
            "template-file4",
        ]);

        var sut = Resolve<TemplateConfigCreator>();
        sut.Create(settings);

        Fs.AllFiles.Should()
            .Contain([
                Paths.ConfigsDirectory.File("template-file1.yml").FullName,
                Paths.ConfigsDirectory.File("template-file2.yml").FullName,
            ]);
    }

    [Test]
    public async Task Force_replaces_existing_file()
    {
        const string templatesJson = """
            {
              "radarr": [
                {
                  "template": "existing-template.yml",
                  "id": "existing-template"
                }
              ],
              "sonarr": []
            }
            """;

        var mockRepoPath = SetupTemplateRepo(templatesJson);
        Fs.AddFile(mockRepoPath.File("existing-template.yml"), new MockFileData("new content"));

        // Create existing file in configs directory with different content
        var existingFile = Paths.ConfigsDirectory.File("existing-template.yml");
        Fs.AddFile(existingFile, new MockFileData("old content"));

        var factory = Resolve<ProviderInitializationFactory>();
        await factory.InitializeProvidersAsync(null, CancellationToken.None);

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns(["existing-template"]);
        settings.Force.Returns(true);

        var sut = Resolve<TemplateConfigCreator>();
        sut.Create(settings);

        (await Fs.File.ReadAllTextAsync(existingFile.FullName)).Should().Be("new content");
    }
}
