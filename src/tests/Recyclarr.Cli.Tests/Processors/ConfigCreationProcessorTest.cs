using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.ExceptionTypes;

namespace Recyclarr.Cli.Tests.Processors;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigCreationProcessorTest : CliIntegrationFixture
{
    [Test]
    public async Task Config_file_created_when_using_default_path()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        await sut.Process(null);

        var file = Fs.GetFile(Paths.ConfigPath);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public async Task Config_file_created_when_using_user_specified_path()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var ymlPath = Fs.CurrentDirectory()
            .SubDirectory("user")
            .SubDirectory("specified")
            .File("file.yml");

        await sut.Process(ymlPath.FullName);

        var file = Fs.GetFile(ymlPath);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public async Task Should_throw_if_file_already_exists()
    {
        var sut = Resolve<ConfigCreationProcessor>();

        var yml = Fs.CurrentDirectory().File("file.yml");
        Fs.AddEmptyFile(yml);

        var act = () => sut.Process(yml.FullName);

        await act.Should().ThrowAsync<FileExistsException>();
    }
}
