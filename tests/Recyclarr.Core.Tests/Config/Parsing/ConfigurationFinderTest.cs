using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Platform;

namespace Recyclarr.Core.Tests.Config.Parsing;

internal sealed class ConfigurationFinderTest
{
    private readonly MockFileSystem _fs;
    private readonly AppPaths _paths;
    private readonly ConfigurationFinder _sut;

    public ConfigurationFinderTest()
    {
        _fs = new MockFileSystem();
        var root = _fs.CurrentDirectory();
        _paths = new AppPaths(
            root.SubDirectory("config"),
            root.SubDirectory("data"),
            root.SubDirectory("state")
        );
        _sut = new ConfigurationFinder(_paths);
    }

    private IFileInfo[] GetYamlPaths()
    {
        return
        [
            _paths.ConfigDirectory.File("recyclarr.yml"),
            _paths.YamlConfigDirectory.File("b.yml"),
            _paths.YamlConfigDirectory.File("c.yaml"),
        ];
    }

    [Test]
    public void Use_default_configs_if_explicit_list_null()
    {
        var yamlPaths = GetYamlPaths();

        foreach (var path in yamlPaths)
        {
            _fs.AddFile(path.FullName, new MockFileData(""));
        }

        var result = _sut.GetConfigFiles();

        result.Should().BeEquivalentTo(yamlPaths, o => o.Including(x => x.FullName));
    }

    [Test]
    public void Use_default_configs_if_explicit_list_empty()
    {
        var yamlPaths = GetYamlPaths();

        foreach (var path in yamlPaths)
        {
            _fs.AddEmptyFile(path);
        }

        var result = _sut.GetConfigFiles();

        result.Should().BeEquivalentTo(yamlPaths, o => o.Including(x => x.FullName));
    }

    [Test]
    public void No_recyclarr_yml_when_not_exists()
    {
        var testFile = _paths.YamlConfigDirectory.File("test.yml");
        _fs.AddEmptyFile(testFile);

        var result = _sut.GetConfigFiles();

        result.Should().ContainSingle(x => x.FullName == testFile.FullName);
    }

    [Test]
    public void Only_add_recyclarr_yml_when_exists()
    {
        var configFile = _paths.ConfigDirectory.File("recyclarr.yml");
        _fs.AddEmptyFile(configFile);

        var result = _sut.GetConfigFiles();

        result.Should().ContainSingle(x => x.FullName == configFile.FullName);
    }

    [Test]
    public void Throw_when_no_configs_found()
    {
        var act = () => _sut.GetConfigFiles();

        act.Should().Throw<NoConfigurationFilesException>();
    }
}
