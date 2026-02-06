using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Recyclarr.Cli.Console.Helpers;

internal class ProviderProgressHandler(IAnsiConsole console, ProviderInitializationFactory factory)
{
    public async Task InitializeProvidersAsync(bool silent, CancellationToken ct)
    {
        var progress = console.Progress();
        if (silent)
        {
            progress.UseRenderHook((_, _) => new EmptyRenderable());
        }

        await progress
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Initializing Resource Providers");
                var reporter = new ProgressReporter(console, task);
                await factory.InitializeProvidersAsync(reporter, ct);
            });
    }

    private class ProgressReporter(IAnsiConsole console, ProgressTask task)
        : IProgress<ProviderProgress>
    {
        public void Report(ProviderProgress value)
        {
            switch (value.Status)
            {
                case ProviderStatus.Starting:
                    task.MaxValue = value.TotalProviders ?? 1;
                    break;

                case ProviderStatus.Processing:
                    break;

                case ProviderStatus.Completed:
                    task.Increment(1);
                    break;

                case ProviderStatus.Failed:
                    task.Increment(1);
                    console.MarkupLine(
                        $"[red]Failed: {value.ProviderName} - {value.ErrorMessage}[/]"
                    );
                    break;
            }
        }
    }

    private sealed class EmptyRenderable : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(0, 0);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => [];
    }
}
