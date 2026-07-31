using System.IO.Abstractions;
using Recyclarr.Config;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigFileCreatorIntegrationTest : CoreIntegrationTestFixture
{
    [Test]
    public void Config_file_created_when_using_default_path()
    {
        var sut = Resolve<ConfigFileCreator>();
        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Path.Returns((string?)null);

        sut.Create(settings);

        var file = Fs.GetFile(Paths.ConfigDirectory.File("recyclarr.yml"));
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Config_file_created_when_using_user_specified_path()
    {
        var sut = Resolve<ConfigFileCreator>();
        var path = Fs.CurrentDirectory()
            .SubDirectory("user")
            .SubDirectory("specified")
            .File("file.yml")
            .FullName;
        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Path.Returns(path);

        sut.Create(settings);

        var file = Fs.GetFile(path);
        file.Should().NotBeNull();
        file.Contents.Should().NotBeEmpty();
    }

    [Test]
    public void Should_throw_if_file_already_exists()
    {
        var sut = Resolve<ConfigFileCreator>();
        var path = Fs.CurrentDirectory().File("file.yml").FullName;
        var settings = Substitute.For<ICreateConfigSettings>();
        settings.Path.Returns(path);
        Fs.AddEmptyFile(path);

        var act = () => sut.Create(settings);

        act.Should().Throw<FileExistsException>();
    }
}
