using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using Common;
using FluentAssertions;
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
    public void All_directories_are_created(
        [Frozen] IEnvironment env,
        [Frozen] IAppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        SonarrCommand cmd,
        InitializeAppDataPath sut)
    {
        sut.Initialize(cmd);

        var expectedDirs = new[]
        {
            paths.LogDirectory,
            paths.RepoDirectory,
            paths.CacheDirectory
        };

        fs.AllDirectories.Select(x => fs.Path.GetFileName(x))
            .Should().IntersectWith(expectedDirs);
    }
}
