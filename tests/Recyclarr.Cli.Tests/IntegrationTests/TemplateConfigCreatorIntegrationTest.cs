using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class TemplateConfigCreatorIntegrationTest : CliIntegrationFixture
{
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
        var mockRepoPath = Paths
            .ReposDirectory.SubDirectory("config-templates")
            .SubDirectory("git")
            .SubDirectory("official");
        Fs.AddDirectory(mockRepoPath);
        Fs.AddFile(mockRepoPath.File("templates.json"), new MockFileData(templatesJson));
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
}
