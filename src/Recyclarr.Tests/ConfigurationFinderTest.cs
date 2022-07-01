using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using Common;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.TestLibrary;

namespace Recyclarr.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationFinderTest
{
    [Test, AutoMockData]
    public void Return_path_next_to_executable_if_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        ConfigurationFinder sut)
    {
        var basePath = fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");
        var baseYaml = basePath.File("recyclarr.yml");

        appContext.BaseDirectory.Returns(basePath.FullName);
        fs.AddFile(baseYaml.FullName, new MockFileData(""));

        var path = sut.FindConfigPath();

        path.FullName.Should().Be(baseYaml.FullName);
    }

    [Test, AutoMockData]
    public void Return_app_data_dir_location_if_base_directory_location_not_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        ConfigurationFinder sut)
    {
        var path = sut.FindConfigPath();

        path.FullName.Should().Be(paths.ConfigPath.FullName);
    }

    [Test, AutoMockData]
    public void Return_base_directory_location_if_both_files_are_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        ConfigurationFinder sut)
    {
        var appPath = fs.CurrentDirectory().SubDirectory("app").SubDirectory("data");
        var basePath = fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");
        var baseYaml = basePath.File("recyclarr.yml");
        var appYaml = appPath.File("recyclarr.yml");

        appContext.BaseDirectory.Returns(basePath.FullName);
        fs.AddFile(baseYaml.FullName, new MockFileData(""));
        fs.AddFile(appYaml.FullName, new MockFileData(""));

        var path = sut.FindConfigPath();

        path.FullName.Should().Be(baseYaml.FullName);
    }
}
