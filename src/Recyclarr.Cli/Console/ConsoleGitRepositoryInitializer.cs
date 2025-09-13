using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Logging;
using Recyclarr.ResourceProviders.Git;
using Spectre.Console;

namespace Recyclarr.Cli.Console;

internal record ProgressState(ProgressTask OverallTask)
{
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
}

[SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Intentionally catching all exceptions to prevent one repository failure from stopping initialization of other repositories"
)]
internal class ConsoleGitRepositoryInitializer(
    IAnsiConsole console,
    IGitRepositoryService gitRepositoryService
)
{
    public async Task InitializeGitRepositories(
        IConsoleOutputSettings outputSettings,
        CancellationToken token = default
    )
    {
        if (outputSettings.IsRawOutputEnabled)
        {
            await InitializeWithoutProgress(token);
            return;
        }

        console.MarkupLine("[cyan]Initializing Git Repositories...[/]");
        console.WriteLine();

        await console
            .Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(ctx => InitializeWithProgressAsync(ctx, token));
    }

    private async Task InitializeWithProgressAsync(ProgressContext ctx, CancellationToken token)
    {
        var overallTask = ctx.AddTask("[yellow]Cloning/Updating Repositories[/]");
        var state = new ProgressState(overallTask);

        var progress = new Progress<RepositoryProgress>(repoProgress =>
            HandleRepositoryProgress(repoProgress, state)
        );

        try
        {
            await gitRepositoryService.InitializeAsync(progress, token);
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Git repository initialization failed: {ex.Message}[/]");
        }
    }

    private void HandleRepositoryProgress(RepositoryProgress repoProgress, ProgressState state)
    {
        var repoName = $"{repoProgress.RepositoryType}/{repoProgress.RepositoryName}";

        switch (repoProgress.Status)
        {
            case RepositoryProgressStatus.Processing:
                state.TotalCount++;
                state.OverallTask.MaxValue = state.TotalCount;
                // No logging for intermediate states
                break;

            case RepositoryProgressStatus.Completed:
                state.CompletedCount++;
                state.OverallTask.Increment(1);
                console.MarkupLine($"[green]✓[/] {repoName}");

                if (state.CompletedCount == state.TotalCount)
                {
                    state.OverallTask.Description = "[green]All Repositories Completed[/]";
                    state.OverallTask.StopTask();
                }
                break;

            case RepositoryProgressStatus.Failed:
                state.CompletedCount++;
                state.OverallTask.Increment(1);
                console.MarkupLine($"[red]✗[/] {repoName} - {repoProgress.ErrorMessage}");

                if (state.CompletedCount == state.TotalCount)
                {
                    state.OverallTask.Description =
                        "[yellow]Repository Initialization Complete (with errors)[/]";
                    state.OverallTask.StopTask();
                }
                break;
        }
    }

    private async Task InitializeWithoutProgress(CancellationToken token)
    {
        try
        {
            await gitRepositoryService.InitializeAsync(null, token);
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Git repository initialization failed: {ex.Message}[/]");
        }
    }
}
