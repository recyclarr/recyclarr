using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Platform;

namespace Recyclarr.Tests.Config.Parsing;

[TestFixture]
public class ConfigurationFinderTest
{
    private static IFileInfo[] GetYamlPaths(IAppPaths paths)
    {
        return new[]
        {
            paths.AppDataDirectory.File("recyclarr.yml"),
            paths.ConfigsDirectory.File("b.yml"),
            paths.ConfigsDirectory.File("c.yaml")
        };
    }

    [Test, AutoMockData]
    public void Use_default_configs_if_explicit_list_null(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        foreach (var path in yamlPaths)
        {
            fs.AddFile(path.FullName, new MockFileData(""));
        }

        var result = sut.GetConfigFiles();

        result.Should().BeEquivalentTo(yamlPaths, o => o.Including(x => x.FullName));
    }

    [Test, AutoMockData]
    public void Use_default_configs_if_explicit_list_empty(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        foreach (var path in yamlPaths)
        {
            fs.AddEmptyFile(path);
        }

        var result = sut.GetConfigFiles();

        result.Should().BeEquivalentTo(yamlPaths, o => o.Including(x => x.FullName));
    }

    [Test, AutoMockData]
    public void No_recyclarr_yml_when_not_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var testFile = paths.ConfigsDirectory.File("test.yml");
        fs.AddEmptyFile(testFile);

        var result = sut.GetConfigFiles();

        result.Should().ContainSingle(x => x.FullName == testFile.FullName);
    }

    [Test, AutoMockData]
    public void Only_add_recyclarr_yml_when_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var configFile = paths.AppDataDirectory.File("recyclarr.yml");
        fs.AddEmptyFile(configFile);

        var result = sut.GetConfigFiles();

        result.Should().ContainSingle(x => x.FullName == configFile.FullName);
    }

    [Test, AutoMockData]
    public void Throw_when_no_configs_found(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var act = () => sut.GetConfigFiles();

        act.Should().Throw<NoConfigurationFilesException>();
    }
}
