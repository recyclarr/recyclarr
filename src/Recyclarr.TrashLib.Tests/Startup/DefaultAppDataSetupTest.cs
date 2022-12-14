using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Common;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Tests.Startup;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DefaultAppDataSetupTest
{
    [Test, AutoMockData]
    public void Initialize_using_default_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);

        var basePath = fs.CurrentDirectory()
            .SubDirectory("base")
            .SubDirectory("path");

        env.GetFolderPath(default, default).ReturnsForAnyArgs(basePath.FullName);

        var paths = sut.CreateAppPaths();

        paths.AppDataDirectory.FullName.Should().Be(basePath.SubDirectory("recyclarr").FullName);
    }

    [Test, AutoMockData]
    public void Initialize_using_path_override(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        DefaultAppDataSetup sut)
    {
        var overridePath = fs.CurrentDirectory()
            .SubDirectory("override")
            .SubDirectory("path");

        var paths = sut.CreateAppPaths(overridePath.FullName);

        paths.AppDataDirectory.FullName.Should().Be(overridePath.FullName);
    }

    [Test, AutoMockData]
    public void Force_creation_uses_correct_behavior_when_disabled(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        var overridePath = fs.CurrentDirectory()
            .SubDirectory("override")
            .SubDirectory("path");

        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);
        env.GetFolderPath(default).ReturnsForAnyArgs(overridePath.FullName);

        sut.CreateAppPaths(null, false);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.None);
        fs.AllDirectories.Should().NotContain(overridePath.FullName);
    }

    [Test, AutoMockData]
    public void Force_creation_uses_correct_behavior_when_enabled(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        var overridePath = fs.CurrentDirectory()
            .SubDirectory("override")
            .SubDirectory("path");

        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?) null);
        env.GetFolderPath(default).ReturnsForAnyArgs(overridePath.FullName);

        sut.CreateAppPaths();

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        fs.AllDirectories.Should().NotContain(overridePath.FullName);
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

        sut.CreateAppPaths();

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

        sut.CreateAppPaths(expectedPath);

        env.DidNotReceiveWithAnyArgs().GetEnvironmentVariable(default!);
        fs.AllDirectories.Should().Contain(expectedPath);
    }
}
