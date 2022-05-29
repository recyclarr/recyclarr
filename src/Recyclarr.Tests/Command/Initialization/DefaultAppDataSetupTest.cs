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
        sut.SetupDefaultPath(null, false);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.None);
    }

    [Test, AutoMockData]
    public void Force_creation_uses_correct_behavior_when_enabled(
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut)
    {
        sut.SetupDefaultPath(null, true);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
    }
}
