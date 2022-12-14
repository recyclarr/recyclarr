using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Cli.Command;
using Recyclarr.Cli.TestLibrary;

// ReSharper disable MethodHasAsyncOverload

namespace Recyclarr.Cli.Tests.Command;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CreateConfigCommandTest : IntegrationFixture
{
    [Test]
    public async Task Config_file_created_when_using_default_path()
    {
        var sut = new CreateConfigCommand();

        await sut.Process(Container);

        var file = Fs.GetFile(Paths.ConfigPath.FullName);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public async Task Config_file_created_when_using_user_specified_path()
    {
        var sut = new CreateConfigCommand();
        var ymlPath = Fs.CurrentDirectory()
            .SubDirectory("user")
            .SubDirectory("specified")
            .File("file.yml").FullName;

        sut.AppDataDirectory = ymlPath;
        await sut.Process(Container);

        var file = Fs.GetFile(ymlPath);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }
}
