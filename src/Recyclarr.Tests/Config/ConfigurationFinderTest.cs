using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using CliFx.Exceptions;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Config;
using TestLibrary.AutoFixture;
using TrashLib;
using TrashLib.Startup;

namespace Recyclarr.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationFinderTest
{
    private static string[] GetYamlPaths(IAppPaths paths)
    {
        return new[]
        {
            paths.ConfigPath.FullName,
            paths.ConfigsDirectory.File("b.yml").FullName,
            paths.ConfigsDirectory.File("c.yml").FullName
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
            fs.AddFile(path, new MockFileData(""));
        }

        var result = sut.GetConfigFiles(null);

        result.Should().BeEquivalentTo(yamlPaths);
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
            fs.AddFile(path, new MockFileData(""));
        }

        var result = sut.GetConfigFiles(new List<string>());

        result.Should().BeEquivalentTo(yamlPaths);
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
            fs.AddFile(path, new MockFileData(""));
        }

        var manualConfig = fs.CurrentDirectory().File("manual-config.yml");
        fs.AddFile(manualConfig.FullName, new MockFileData(""));

        var result = sut.GetConfigFiles(new[] {manualConfig.FullName});

        result.Should().BeEquivalentTo(manualConfig.FullName);
    }

    [Test, AutoMockData]
    public void Non_existent_files_are_skipped(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        fs.AddFile(yamlPaths[0], new MockFileData(""));
        fs.AddFile(yamlPaths[1], new MockFileData(""));

        var result = sut.GetConfigFiles(yamlPaths);

        result.Should().BeEquivalentTo(yamlPaths.Take(2));
    }

    [Test, AutoMockData]
    public void No_recyclarr_yml_when_not_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var testFile = paths.ConfigsDirectory.File("test.yml").FullName;
        fs.AddFile(testFile, new MockFileData(""));

        var result = sut.GetConfigFiles(Array.Empty<string>());

        result.Should().BeEquivalentTo(testFile);
    }

    [Test, AutoMockData]
    public void Only_add_recyclarr_yml_when_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        fs.AddFile(paths.ConfigPath.FullName, new MockFileData(""));

        var result = sut.GetConfigFiles(Array.Empty<string>());

        result.Should().BeEquivalentTo(paths.ConfigPath.FullName);
    }

    [Test, AutoMockData]
    public void Throw_when_no_configs_found(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        ConfigurationFinder sut)
    {
        var act = () => sut.GetConfigFiles(Array.Empty<string>());

        act.Should().Throw<CommandException>();
    }
}
