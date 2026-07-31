using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Processors;
using Recyclarr.Servarr.CustomFormat;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Delete things from services like Radarr and Sonarr")]
[UsedImplicitly]
internal class DeleteCustomFormatsCommand(
    ProviderProgressHandler providerProgressHandler,
    ConfigPipelineFactory configPipelineFactory,
    IAnsiConsole console,
    ILogger log
) : AsyncCommand<DeleteCustomFormatsCommand.CliSettings>
{
    [UsedImplicitly]
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings, IDeleteCustomFormatSettings
    {
        [CommandArgument(0, "<instance_name>")]
        [Description("The name of the instance to delete CFs from.")]
        public string InstanceName { get; init; } = "";

        [CommandArgument(0, "[cf_names]")]
        [Description(
            "One or more custom format names to delete. Optional only if `--all` is used."
        )]
        public string[] CustomFormatNamesOption { get; init; } = [];
        public IReadOnlyCollection<string> CustomFormatNames => CustomFormatNamesOption;

        [CommandOption("-a|--all")]
        [Description("Delete ALL custom formats.")]
        public bool All { get; init; } = false;

        [CommandOption("-f|--force")]
        [Description("Perform the delete operation with NO confirmation prompt.")]
        public bool Force { get; init; } = false;

        [CommandOption("-p|--preview")]
        [Description("Preview what custom formats will be deleted without actually deleting them.")]
        public bool Preview { get; init; } = false;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        await providerProgressHandler.InitializeProvidersAsync(silent: false, ct);

        await configPipelineFactory
            .FromDefaultPaths()
            .FilterByInstance([settings.InstanceName])
            .ProcessEach<ICustomFormatDeleter>(
                async (deleter, token) =>
                {
                    var candidates = await deleter.GetCandidatesAsync(settings, token);

                    if (candidates.Count == 0)
                    {
                        console.MarkupLine(
                            "[yellow]Done[/]: No custom formats found or specified to delete."
                        );
                        return;
                    }

                    PrintPreview(candidates);

                    if (settings.Preview)
                    {
                        console.MarkupLine(
                            "This is a preview! [u]No actual deletions will be performed.[/]"
                        );
                        return;
                    }

                    if (
                        !settings.Force
                        && !await console.ConfirmAsync(
                            "\nAre you sure you want to [bold red]permanently delete[/] the above custom formats?",
                            cancellationToken: token
                        )
                    )
                    {
                        console.WriteLine("Aborted!");
                        return;
                    }

                    var summary = await deleter.DeleteAsync(candidates, token);
                    RenderSummary(summary);
                },
                ct
            );

        return (int)ExitStatus.Succeeded;
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private void PrintPreview(IReadOnlyList<CustomFormatDeleteItem> candidates)
    {
        console.MarkupLine("The following custom formats will be [bold red]DELETED[/]:");
        console.WriteLine();

        var cfNames = candidates
            .Select(x => x.Name)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .Chunk(Math.Max(15, candidates.Count / 3)) // Minimum row size is 15 for the table
            .ToList();

        var grid = new Grid().AddColumns(cfNames.Count);

        foreach (var rowItems in cfNames.Transpose())
        {
            var rows = rowItems
                .Select(x =>
                    Markup.FromInterpolated(CultureInfo.InvariantCulture, $"[bold white]{x}[/]")
                )
                .ToArray();

            grid.AddRow(rows);
        }

        console.Write(grid);
        console.WriteLine();
    }

    private void RenderSummary(CustomFormatDeleteSummary summary)
    {
        if (summary.Failed == 0)
        {
            console.MarkupLineInterpolated($"[green]Deleted {summary.Deleted} custom formats[/]");
        }
        else if (summary.Deleted == 0)
        {
            console.MarkupLineInterpolated(
                $"[red]Failed to delete all {summary.Failed} custom formats[/]"
            );
        }
        else
        {
            console.MarkupLineInterpolated(
                $"[yellow]Deleted {summary.Deleted} custom formats ({summary.Failed} failed)[/]"
            );
        }

        // Log failures for diagnostics
        if (summary.FailedNames.Count > 0)
        {
            log.Error("Failed to delete custom formats: {@Names}", summary.FailedNames);
        }
    }
}
