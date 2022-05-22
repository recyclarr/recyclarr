using AutoFixture.NUnit3;
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
    public void Do_not_override_path_if_null(
        [Frozen] IAppPaths paths,
        SonarrCommand cmd,
        InitializeAppDataPath sut)
    {
        sut.Initialize(cmd);

        paths.DidNotReceiveWithAnyArgs().SetAppDataPath(default!);
    }

    [Test, AutoMockData]
    public void Override_path_if_not_null(
        [Frozen] IAppPaths paths,
        SonarrCommand cmd,
        InitializeAppDataPath sut)
    {
        cmd.AppDataDirectory = "path";
        sut.Initialize(cmd);

        paths.Received().SetAppDataPath("path");
    }
}
