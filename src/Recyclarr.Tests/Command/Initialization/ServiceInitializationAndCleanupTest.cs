using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.Command.Initialization;
using Recyclarr.Command.Initialization.Cleanup;
using Recyclarr.Command.Initialization.Init;
using TestLibrary.AutoFixture;

namespace Recyclarr.Tests.Command.Initialization;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceInitializationAndCleanupTest
{
    [Test, AutoMockData]
    public async Task Cleanup_happens_when_exception_occurs_in_action(
        ServiceCommand cmd,
        IServiceCleaner cleaner)
    {
        var sut = new ServiceInitializationAndCleanup(
            Enumerable.Empty<IServiceInitializer>().OrderBy(_ => 1),
            new[] {cleaner}.OrderBy(_ => 1));

        var act = () => sut.Execute(cmd, () => throw new NullReferenceException());

        await act.Should().ThrowAsync<NullReferenceException>();
        cleaner.Received().Cleanup();
    }

    [Test, AutoMockData]
    public async Task Cleanup_happens_when_exception_occurs_in_init(
        ServiceCommand cmd,
        IServiceInitializer init,
        IServiceCleaner cleaner)
    {
        var sut = new ServiceInitializationAndCleanup(
            new[] {init}.OrderBy(_ => 1),
            new[] {cleaner}.OrderBy(_ => 1));

        init.WhenForAnyArgs(x => x.Initialize(default!))
            .Do(_ => throw new NullReferenceException());

        var act = () => sut.Execute(cmd, () => Task.CompletedTask);

        await act.Should().ThrowAsync<NullReferenceException>();
        cleaner.Received().Cleanup();
    }
}
