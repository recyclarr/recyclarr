using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Platform;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

internal sealed class ConfigIncludeProcessorTest
{
    private readonly MockFileSystem _fs = new();
    private readonly IAppPaths _paths = Substitute.For<IAppPaths>();

    private ConfigIncludeProcessor CreateSut()
    {
        _paths.YamlIncludeDirectory.Returns(_fs.CurrentDirectory().SubDirectory("includes"));
        return new ConfigIncludeProcessor(
            _fs,
            _paths,
            new ConfigInputRecorder(),
            Substitute.For<ILogger>()
        );
    }

    [Test]
    public void Throw_when_null_include_path()
    {
        var sut = CreateSut();
        var includeDirective = new ConfigYamlInclude { Config = null };

        var act = () => sut.GetPathToConfig(includeDirective, default);

        act.Should().Throw<YamlIncludeException>();
    }

    [Test]
    public void Get_relative_config_include_path()
    {
        var sut = CreateSut();
        _fs.AddEmptyFile(_paths.YamlIncludeDirectory.File("foo/bar/config.yml"));

        var includeDirective = new ConfigYamlInclude { Config = "foo/bar/config.yml" };

        var path = sut.GetPathToConfig(includeDirective, default);

        path.FullName.Should().Be(_paths.YamlIncludeDirectory.File("foo/bar/config.yml").FullName);
    }

    [Test]
    public void Get_absolute_config_include_path()
    {
        var sut = CreateSut();
        var absolutePath = _fs.CurrentDirectory().File("foo/bar/config.yml");
        _fs.AddEmptyFile(absolutePath);

        var includeDirective = new ConfigYamlInclude { Config = absolutePath.FullName };

        var path = sut.GetPathToConfig(includeDirective, default);

        path.FullName.Should().Be(absolutePath.FullName);
    }

    [Test]
    public void Throw_when_relative_config_include_path_does_not_exist()
    {
        var sut = CreateSut();
        var includeDirective = new ConfigYamlInclude { Config = "foo/bar/config.yml" };

        var act = () => sut.GetPathToConfig(includeDirective, default);

        act.Should().Throw<YamlIncludeException>();
    }

    [Test]
    public void Throw_when_absolute_config_include_path_does_not_exist()
    {
        var sut = CreateSut();
        var absolutePath = _fs.CurrentDirectory().File("foo/bar/config.yml");

        var includeDirective = new ConfigYamlInclude { Config = absolutePath.FullName };

        var act = () => sut.GetPathToConfig(includeDirective, default);

        act.Should().Throw<YamlIncludeException>();
    }
}
