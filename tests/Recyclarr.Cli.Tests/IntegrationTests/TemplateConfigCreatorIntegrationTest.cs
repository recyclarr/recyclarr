using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Cli.Tests.IntegrationTests;

[CliDataSource]
internal sealed class TemplateConfigCreatorIntegrationTest(
    TemplateConfigCreator sut,
    ProviderInitializationFactory providerFactory,
    MockFileSystem fs,
    IAppPaths paths
)
{
    private IDirectoryInfo SetupTemplateRepo(string templatesJson)
    {
        var mockRepoPath = paths
            .ResourceDirectory.SubDirectory("config-templates")
            .SubDirectory("git")
            .SubDirectory("official");
        fs.AddDirectory(mockRepoPath);
        fs.AddFile(mockRepoPath.File("templates.json"), new MockFileData(templatesJson));
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

        sut.Create(settings);

        fs.AllFiles.Should()
            .Contain([
                paths.YamlConfigDirectory.File("template-file1.yml").FullName,
                paths.YamlConfigDirectory.File("template-file2.yml").FullName,
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
        fs.AddFile(mockRepoPath.File("existing-template.yml"), new MockFileData("new content"));

        // Create existing file in configs directory with different content
        var existingFile = paths.YamlConfigDirectory.File("existing-template.yml");
        fs.AddFile(existingFile, new MockFileData("old content"));

        await providerFactory.InitializeProvidersAsync(null, CancellationToken.None);

        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Templates.Returns(["existing-template"]);
        settings.Force.Returns(true);

        sut.Create(settings);

        (await fs.File.ReadAllTextAsync(existingFile.FullName)).Should().Be("new content");
    }
}
