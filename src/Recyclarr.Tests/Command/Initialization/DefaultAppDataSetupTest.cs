using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using Common;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command.Initialization;
using TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib;

namespace Recyclarr.Tests.Command.Initialization;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DefaultAppDataSetupTest
{
    [Test, AutoMockData]
    public void Initialize_using_default_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        [Frozen] IAppPaths paths,
        DefaultAppDataSetup sut)
    {
        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);

        paths.DefaultAppDataDirectoryName.Returns("app_data");
        env.GetFolderPath(Arg.Any<Environment.SpecialFolder>(), Arg.Any<Environment.SpecialFolderOption>())
            .Returns(FileUtils.NormalizePath("base/path"));

        sut.SetupDefaultPath(null, false);

        paths.Received().SetAppDataPath(FileUtils.NormalizePath("base/path/app_data"));
    }

    [Test, AutoMockData]
    public void Initialize_using_path_override(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        DefaultAppDataSetup sut)
    {
        var overridePath = FileUtils.NormalizePath("/override/path");
        sut.SetupDefaultPath(overridePath, false);

        paths.Received().SetAppDataPath(overridePath);
        fs.AllDirectories.Should().Contain(overridePath);
    }

    [Test, AutoMockData]
    public void Force_creation_uses_correct_behavior_when_disabled(
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);

        sut.SetupDefaultPath(null, false);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.None);
    }

    [Test, AutoMockData]
    public void Force_creation_uses_correct_behavior_when_enabled(
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);

        sut.SetupDefaultPath(null, true);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
    }

    [Test, AutoMockData]
    public void Use_environment_variable_if_override_not_specified(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        var expectedPath = fs.CurrentDirectory()
            .SubDirectory("env")
            .SubDirectory("var")
            .SubDirectory("path").FullName;

        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs(expectedPath);

        sut.SetupDefaultPath(null, true);

        env.Received().GetEnvironmentVariable("RECYCLARR_APP_DATA");
        fs.AllDirectories.Should().Contain(expectedPath);
    }

    [Test, AutoMockData]
    public void Explicit_override_takes_precedence_over_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        var expectedPath = fs.CurrentDirectory()
            .SubDirectory("env")
            .SubDirectory("var")
            .SubDirectory("path").FullName;

        sut.SetupDefaultPath(expectedPath, true);

        env.DidNotReceiveWithAnyArgs().GetEnvironmentVariable(default!);
        fs.AllDirectories.Should().Contain(expectedPath);
    }
}
