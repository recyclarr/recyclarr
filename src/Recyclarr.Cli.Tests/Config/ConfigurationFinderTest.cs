using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationFinderTest
{
    private static IFileInfo[] GetYamlPaths(IAppPaths paths)
    {
        return new[]
        {
            paths.ConfigPath,
            paths.ConfigsDirectory.File("b.yml"),
            paths.ConfigsDirectory.File("c.yml")
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

        var result = sut.GetConfigFiles(null);

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

        var result = sut.GetConfigFiles(new List<IFileInfo>());

        result.Should().BeEquivalentTo(yamlPaths, o => o.Including(x => x.FullName));
    }

    [Test, AutoMockData]
    public void Use_explicit_paths_instead_of_default(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        foreach (var path in yamlPaths)
        {
            fs.AddFile(path.FullName, new MockFileData(""));
        }

        var manualConfig = fs.CurrentDirectory().File("manual-config.yml");
        fs.AddEmptyFile(manualConfig);

        var result = sut.GetConfigFiles(new[] {manualConfig});

        result.Should().ContainSingle(x => x.FullName == manualConfig.FullName);
    }

    [Test, AutoMockData]
    public void No_recyclarr_yml_when_not_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var testFile = paths.ConfigsDirectory.File("test.yml");
        fs.AddEmptyFile(testFile);

        var result = sut.GetConfigFiles(Array.Empty<IFileInfo>());

        result.Should().ContainSingle(x => x.FullName == testFile.FullName);
    }

    [Test, AutoMockData]
    public void Only_add_recyclarr_yml_when_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        fs.AddEmptyFile(paths.ConfigPath);

        var result = sut.GetConfigFiles(Array.Empty<IFileInfo>());

        result.Should().ContainSingle(x => x.FullName == paths.ConfigPath.FullName);
    }

    [Test, AutoMockData]
    public void Throw_when_no_configs_found(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var act = () => sut.GetConfigFiles(Array.Empty<IFileInfo>());

        act.Should().Throw<NoConfigurationFilesException>();
    }
}
