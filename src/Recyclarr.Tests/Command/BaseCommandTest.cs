using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using CliFx.Attributes;
using CliFx.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.TestLibrary;
using TestLibrary.AutoFixture;

namespace Recyclarr.Tests.Command;

[Command]
public class StubBaseCommand : BaseCommand
{
    public override string? AppDataDirectory { get; set; }

    public StubBaseCommand(ICompositionRoot compositionRoot)
    {
        CompositionRoot = compositionRoot;
    }

    public override Task Process(IServiceLocatorProxy container)
    {
        return Task.CompletedTask;
    }
}

[TestFixture]
// Cannot be parallelized due to static CompositionRoot property
public class BaseCommandTest
{
    [Test, AutoMockData]
    public async Task All_directories_are_created(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen(Matching.ImplementedInterfaces)] TestAppPaths paths,
        IConsole console,
        StubBaseCommand sut)
    {
        await sut.ExecuteAsync(console);

        var expectedDirs = new[]
        {
            paths.LogDirectory.FullName,
            paths.RepoDirectory.FullName,
            paths.CacheDirectory.FullName
        };

        expectedDirs.Should().IntersectWith(fs.AllDirectories);
    }
}
