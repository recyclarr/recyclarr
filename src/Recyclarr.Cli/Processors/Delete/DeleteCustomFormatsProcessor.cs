using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Autofac;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Delete;

[UsedImplicitly]
internal class CustomFormatConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public ICustomFormatApiService CustomFormatApi { get; } =
        scope.Resolve<ICustomFormatApiService>();
}

internal class DeleteCustomFormatsProcessor(
    ILogger log,
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    ConfigurationScopeFactory scopeFactory
)
{
    public async Task Process(IDeleteCustomFormatSettings settings, CancellationToken ct)
    {
        var configs = configRegistry.FindAndLoadConfigs(
            new ConfigFilterCriteria { Instances = [settings.InstanceName] }
        );

        if (configs.Count != 1)
        {
            return;
        }

        using var scope = scopeFactory.Start<CustomFormatConfigurationScope>(configs.Single());

        var cfs = await ObtainCustomFormats(scope.CustomFormatApi, ct);

        if (!settings.All)
        {
            if (settings.CustomFormatNames.Count == 0)
            {
                throw new CommandException(
                    "Custom format names must be specified if the `--all` option is not used."
                );
            }

            cfs = ProcessManuallySpecifiedFormats(settings, cfs);
        }

        if (!cfs.Any())
        {
            console.MarkupLine("[yellow]Done[/]: No custom formats found or specified to delete.");
            return;
        }

        PrintPreview(cfs);

        if (settings.Preview)
        {
            console.MarkupLine("This is a preview! [u]No actual deletions will be performed.[/]");
            return;
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
            return;
        }

        await DeleteCustomFormats(scope.CustomFormatApi, cfs);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task DeleteCustomFormats(
        ICustomFormatApiService api,
        ICollection<CustomFormatResource> cfs
    )
    {
        ConcurrentBag<string> successNames = [];
        ConcurrentBag<string> failedNames = [];

        await console
            .Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Deleting Custom Formats").MaxValue(cfs.Count);

                var options = new ParallelOptions { MaxDegreeOfParallelism = 8 };
                await Parallel.ForEachAsync(
                    cfs,
                    options,
                    async (cf, token) =>
                    {
                        try
                        {
                            await api.DeleteCustomFormat(cf.Id, token);
                            successNames.Add(cf.Name);
                        }
                        catch (Exception)
                        {
                            failedNames.Add(cf.Name);
                            console.WriteLine($"Failed to delete CF: {cf.Name}");
                        }

                        task.Increment(1);
                    }
                );
            });

        if (!successNames.IsEmpty)
        {
            log.Debug("Deleted custom formats: {@Names}", successNames);
        }

        if (!failedNames.IsEmpty)
        {
            log.Error("Failed to delete custom formats: {@Names}", failedNames);
        }

        // Print summary to console
        if (failedNames.IsEmpty)
        {
            console.MarkupLineInterpolated(
                $"[green]Deleted {successNames.Count} custom formats[/]"
            );
        }
        else if (successNames.IsEmpty)
        {
            console.MarkupLineInterpolated(
                $"[red]Failed to delete all {failedNames.Count} custom formats[/]"
            );
        }
        else
        {
            console.MarkupLineInterpolated(
                $"[yellow]Deleted {successNames.Count} custom formats ({failedNames.Count} failed)[/]"
            );
        }
    }

    private async Task<IList<CustomFormatResource>> ObtainCustomFormats(
        ICustomFormatApiService api,
        CancellationToken ct
    )
    {
        IList<CustomFormatResource> cfs = [];

        await console
            .Status()
            .StartAsync(
                "Obtaining custom formats...",
                async _ =>
                {
                    cfs = await api.GetCustomFormats(ct);
                }
            );

        return cfs;
    }

    private IList<CustomFormatResource> ProcessManuallySpecifiedFormats(
        IDeleteCustomFormatSettings settings,
        IList<CustomFormatResource> cfs
    )
    {
        ILookup<bool, (string Name, IEnumerable<CustomFormatResource> Cfs)> result = settings
            .CustomFormatNames.GroupJoin(
                cfs,
                x => x,
                x => x.Name,
                (x, y) => (Name: x, Cf: y),
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToLookup(x => x.Cf.Any());

        // 'false' means there were no CFs matched to this CF name
        if (result[false].Any())
        {
            var cfNames = result[false].Select(x => x.Name).ToList();
            log.Debug("Unmatched CFs: {Names}", cfNames);
            foreach (var name in cfNames)
            {
                console.MarkupLineInterpolated(
                    $"[yellow]Warning[/]: Unmatched CF Name: [teal]{name}[/]"
                );
            }
        }

        // 'true' represents CFs that match names provided in user-input (if provided)
        cfs = result[true].SelectMany(x => x.Cfs).ToList();
        return cfs;
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private void PrintPreview(ICollection<CustomFormatResource> cfs)
    {
        console.MarkupLine("The following custom formats will be [bold red]DELETED[/]:");
        console.WriteLine();

        var cfNames = cfs.Select(x => x.Name)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .Chunk(Math.Max(15, cfs.Count / 3)) // Minimum row size is 15 for the table
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
}
