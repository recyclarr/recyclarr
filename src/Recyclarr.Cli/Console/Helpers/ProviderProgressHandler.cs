using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Spectre.Console;

namespace Recyclarr.Cli.Console.Helpers;

internal class ProviderProgressHandler(IAnsiConsole console, ProviderInitializationFactory factory)
{
    public async Task InitializeProvidersAsync(bool silent, CancellationToken ct)
    {
        if (silent)
        {
            await factory.InitializeProvidersAsync(new ProgressReporter(console, task: null), ct);
            return;
        }

        await console
            .Progress()
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

    // Without a task (raw mode), only failures are written; no progress is rendered.
    private class ProgressReporter(IAnsiConsole console, ProgressTask? task)
        : IProgress<ProviderProgress>
    {
        public void Report(ProviderProgress value)
        {
            switch (value.Status)
            {
                case ProviderStatus.Starting:
                    task?.MaxValue = value.TotalProviders ?? 1;
                    break;

                case ProviderStatus.Processing:
                    break;

                case ProviderStatus.Completed:
                    task?.Increment(1);
                    break;

                case ProviderStatus.Failed:
                    var name = Markup.Escape(value.ProviderName);
                    var error = Markup.Escape(value.ErrorMessage ?? "");
                    task?.Increment(1);
                    console.MarkupLine($"[red]Failed: {name} - {error}[/]");
                    break;
            }
        }
    }
}
