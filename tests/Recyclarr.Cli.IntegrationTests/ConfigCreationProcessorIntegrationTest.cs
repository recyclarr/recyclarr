using System.IO.Abstractions;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Repo;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
internal class ConfigCreationProcessorIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void Config_file_created_when_using_default_path()
    {
        var repo = Resolve<IConfigTemplatesRepo>();
        Fs.AddFile(repo.Path.File("templates.json"), new MockFileData("{}"));

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

        var repo = Resolve<IConfigTemplatesRepo>();
        Fs.AddFile(repo.Path.File("templates.json"), new MockFileData(templatesJson));
        Fs.AddEmptyFile(repo.Path.File("template-file1.yml"));
        Fs.AddEmptyFile(repo.Path.File("template-file2.yml"));
        // This one shouldn't show up in the result because the user didn't ask for it
        Fs.AddEmptyFile(repo.Path.File("template-file3.yml"));

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
