using AutoFixture.NUnit3;
using Common;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.Command.Initialization.Init;
using TestLibrary.AutoFixture;
using TrashLib;

namespace Recyclarr.Tests.Command.Initialization.Init;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class InitializeAppDataPathTest
{
    [Test, AutoMockData]
    public void Use_default_app_data_if_not_specified(
        [Frozen] IEnvironment env,
        [Frozen] IAppPaths paths,
        SonarrCommand cmd,
        InitializeAppDataPath sut)
    {
        env.GetFolderPath(Arg.Any<Environment.SpecialFolder>(), Arg.Any<Environment.SpecialFolderOption>())
            .Returns("app_data");

        sut.Initialize(cmd);

        env.Received().GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        paths.Received().SetAppDataPath("app_data");
    }

    [Test, AutoMockData]
    public void Use_specified_app_data_if_user_provided(
        [Frozen] IAppPaths paths,
        SonarrCommand cmd,
        InitializeAppDataPath sut)
    {
        cmd.AppDataDirectory = "path";
        sut.Initialize(cmd);
        paths.Received().SetAppDataPath("path");
    }
}
