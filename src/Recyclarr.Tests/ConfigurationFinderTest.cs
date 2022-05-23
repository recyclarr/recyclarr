using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using Common;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib;

namespace Recyclarr.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationFinderTest
{
    [Test, AutoMockData]
    public void Return_path_next_to_executable_if_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        ConfigurationFinder sut)
    {
        paths.DefaultConfigFilename.Returns("recyclarr.yml");
        paths.ConfigPath.Returns(@"app\data");
        appContext.BaseDirectory.Returns(@"base\path");
        fs.AddFile(@"base\path\recyclarr.yml", new MockFileData(""));

        var path = sut.FindConfigPath();

        path.Should().EndWith(@"base\path\recyclarr.yml");
    }

    [Test, AutoMockData]
    public void Return_app_data_dir_location_if_base_directory_location_not_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        ConfigurationFinder sut)
    {
        paths.ConfigPath.Returns(@"app\data\recyclarr.yml");
        appContext.BaseDirectory.Returns(@"base\path");

        var path = sut.FindConfigPath();

        path.Should().EndWith(@"app\data\recyclarr.yml");
    }

    [Test, AutoMockData]
    public void Return_base_directory_location_if_both_files_are_present(
        [Frozen] IAppContext appContext,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        ConfigurationFinder sut)
    {
        paths.DefaultConfigFilename.Returns("recyclarr.yml");
        paths.ConfigPath.Returns(@"app\data");
        appContext.BaseDirectory.Returns(@"base\path");

        fs.AddFile(@"base\path\recyclarr.yml", new MockFileData(""));
        fs.AddFile(@"app\data\recyclarr.yml", new MockFileData(""));

        var path = sut.FindConfigPath();

        path.Should().EndWith(@"base\path\recyclarr.yml");
    }
}
