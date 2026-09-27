using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Recyclarr.Cli.Server;
using Recyclarr.Client.V1;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[Description("Delete things from services like Radarr and Sonarr")]
[UsedImplicitly]
internal class DeleteCustomFormatsCommand(
    ServerConnectionFactory connections,
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
    internal class CliSettings : BaseCommandSettings
    {
        [CommandArgument(0, "<instance_name>")]
        [Description("The name of the instance to delete CFs from.")]
        public string InstanceName { get; init; } = "";

        [CommandArgument(0, "[cf_names]")]
        [Description(
            "One or more custom format names to delete. Optional only if `--all` is used."
        )]
        public string[] CustomFormatNames { get; init; } = [];

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

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        if (!settings.All && settings.CustomFormatNames.Length == 0)
        {
            const string message =
                "Custom format names must be specified if the `--all` option is not used.";
            console.MarkupLine($"[red]Error:[/] {message}");
            log.Error(message);
            return (int)ExitStatus.Failed;
        }

        await using var connection = await connections.ConnectAsync(ct);
        var response = await connection.Instances.CustomFormatsGet(settings.InstanceName, ct);
        var candidates = SelectCandidates(settings, response.ContentOrThrow().Items);

        if (candidates.Count == 0)
        {
            console.MarkupLine("[yellow]Done[/]: No custom formats found or specified to delete.");
            return (int)ExitStatus.Succeeded;
        }

        PrintPreview(candidates);

        if (settings.Preview)
        {
            console.MarkupLine("This is a preview! [u]No actual deletions will be performed.[/]");
            return (int)ExitStatus.Succeeded;
        }

        if (
            !settings.Force
            && !await console.ConfirmAsync(
                "\nAre you sure you want to [bold red]permanently delete[/] the above custom formats?",
                cancellationToken: ct
            )
        )
        {
            console.WriteLine("Aborted!");
            return (int)ExitStatus.Succeeded;
        }

        var failed = await DeleteAsync(connection.Instances, settings.InstanceName, candidates, ct);
        RenderSummary(candidates.Count - failed.Count, failed);
        return (int)ExitStatus.Succeeded;
    }

    // Names match case-insensitively; names with no match are reported and skipped.
    private List<InstanceCustomFormatSummaryResponse> SelectCandidates(
        CliSettings settings,
        IReadOnlyCollection<InstanceCustomFormatSummaryResponse> customFormats
    )
    {
        if (settings.All)
        {
            return [.. customFormats];
        }

        var names = settings.CustomFormatNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = customFormats.Where(cf => names.Contains(cf.Name)).ToList();

        var unmatched = names.Except(
            candidates.Select(cf => cf.Name),
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var name in unmatched)
        {
            console.MarkupLineInterpolated($"[yellow]Warning:[/] Unmatched CF name: {name}");
            log.Warning("Unmatched CF Name: {Name}", name);
        }

        return candidates;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "One failed delete must not stop the others; failures are reported."
    )]
    private async Task<IReadOnlyList<string>> DeleteAsync(
        IInstancesApi api,
        string instanceName,
        IReadOnlyList<InstanceCustomFormatSummaryResponse> candidates,
        CancellationToken ct
    )
    {
        ConcurrentBag<string> failed = [];
        var options = new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct };
        await Parallel.ForEachAsync(
            candidates,
            options,
            async (cf, token) =>
            {
                try
                {
                    using var result = await api.CustomFormatsDelete(instanceName, cf.Id, token);
                    if (result.Error is not null)
                    {
                        log.Debug(result.Error, "Failed to delete custom format {Name}", cf.Name);
                        failed.Add(cf.Name);
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.Debug(e, "Failed to delete custom format {Name}", cf.Name);
                    failed.Add(cf.Name);
                }
            }
        );

        return [.. failed];
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private void PrintPreview(List<InstanceCustomFormatSummaryResponse> candidates)
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

    private void RenderSummary(int deleted, IReadOnlyList<string> failed)
    {
        if (failed.Count == 0)
        {
            console.MarkupLineInterpolated($"[green]Deleted {deleted} custom formats[/]");
            log.Information("Deleted {Count} custom formats", deleted);
            return;
        }

        if (deleted == 0)
        {
            console.MarkupLineInterpolated(
                $"[red]Failed to delete all {failed.Count} custom formats[/]"
            );
        }
        else
        {
            console.MarkupLineInterpolated(
                $"[yellow]Deleted {deleted} custom formats ({failed.Count} failed)[/]"
            );
        }

        log.Error("Failed to delete custom formats: {@Names}", failed);
    }
}
