using System.Reactive.Linq;
using Recyclarr.Cli.Processors.Sync.Progress;
using Recyclarr.Sync;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Processors.Sync.Progress;

internal sealed class SyncProgressRendererTest
{
    [Test]
    public async Task Non_interactive_console_only_runs_sync_action()
    {
        using var console = new TestConsole();
        console.Profile.Capabilities.Interactive = false;
        var run = Substitute.For<ISyncRunScope>();
        run.Pipelines.Returns(Observable.Never<PipelineEvent>());
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var calls = 0;
        var sut = new SyncProgressRenderer(console, run);

        var renderTask = sut.RenderProgressAsync(
            [],
            async () =>
            {
                calls++;
                await completion.Task;
            },
            CancellationToken.None
        );

        renderTask.IsCompleted.Should().BeFalse();
        calls.Should().Be(1);

        completion.SetResult();
        await renderTask;

        calls.Should().Be(1);
        console.Output.Should().BeEmpty();
    }

    [Test]
    public async Task Interactive_console_renders_progress()
    {
        using var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        var run = Substitute.For<ISyncRunScope>();
        run.Pipelines.Returns(Observable.Never<PipelineEvent>());
        var sut = new SyncProgressRenderer(console, run);

        await sut.RenderProgressAsync(
            ["instance"],
            () => Task.CompletedTask,
            CancellationToken.None
        );

        console.Output.Should().Contain("Legend:").And.Contain("instance");
    }
}
