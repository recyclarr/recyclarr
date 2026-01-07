using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Spectre.Console;

namespace Recyclarr.Cli.Console.Helpers;

internal class ProviderProgressHandler(
    IAnsiConsole console,
    ProviderInitializationFactory factory,
    ProgressFactory progressFactory
)
{
    public async Task InitializeProvidersAsync(CancellationToken ct)
    {
        await progressFactory
            .Create()
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
                var progress = new ProgressReporter(console, task);
                await factory.InitializeProvidersAsync(progress, ct);
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
}
