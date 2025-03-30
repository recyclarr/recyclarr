using System.IO.Abstractions;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ConfigTemplates;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class TemplateConfigCreatorIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Template_id_matching_works()
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

        _ = Resolve<IConfigTemplatesResourceQuery>(); // Ensure resource provider is initialized
        // Create a mock templates.json file in the expected location
        var mockRepoPath = "/repos/config-templates-default";
        Fs.AddDirectory(mockRepoPath);
        Fs.AddFile($"{mockRepoPath}/templates.json", new MockFileData(templatesJson));
        Fs.AddEmptyFile($"{mockRepoPath}/template-file1.yml");
        Fs.AddEmptyFile($"{mockRepoPath}/template-file2.yml");
        // This one shouldn't show up in the result because the user didn't ask for it
        Fs.AddEmptyFile($"{mockRepoPath}/template-file3.yml");

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

        var sut = Resolve<TemplateConfigCreator>();
        sut.Create(settings);

        Fs.AllFiles.Should()
            .Contain(
                [
                    Paths.ConfigsDirectory.File("template-file1.yml").FullName,
                    Paths.ConfigsDirectory.File("template-file2.yml").FullName,
                ]
            );
    }
}
