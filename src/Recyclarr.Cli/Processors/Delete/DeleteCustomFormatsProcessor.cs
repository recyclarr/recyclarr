using System.Diagnostics.CodeAnalysis;
using Autofac;
using JetBrains.Annotations;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Delete;

[UsedImplicitly]
internal class CustomFormatConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public ICustomFormatApiService CustomFormatApi { get; } = scope.Resolve<ICustomFormatApiService>();
}

public class DeleteCustomFormatsProcessor(
    ILogger log,
    IAnsiConsole console,
    IConfigurationRegistry configRegistry,
    ConfigurationScopeFactory scopeFactory)
    : IDeleteCustomFormatsProcessor
{
    public async Task Process(IDeleteCustomFormatSettings settings, CancellationToken ct)
    {
        using var scope = scopeFactory.Start<CustomFormatConfigurationScope>(GetTargetConfig(settings));

        var cfs = await ObtainCustomFormats(scope.CustomFormatApi, ct);

        if (!settings.All)
        {
            if (settings.CustomFormatNames.Count == 0)
            {
                throw new CommandException("Custom format names must be specified if the `--all` option is not used.");
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

        if (!settings.Force &&
            !console.Confirm("\nAre you sure you want to [bold red]permanently delete[/] the above custom formats?"))
        {
            console.WriteLine("Aborted!");
            return;
        }

        await DeleteCustomFormats(scope.CustomFormatApi, cfs);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task DeleteCustomFormats(ICustomFormatApiService api, ICollection<CustomFormatData> cfs)
    {
        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Deleting Custom Formats").MaxValue(cfs.Count);

            var options = new ParallelOptions {MaxDegreeOfParallelism = 8};
            await Parallel.ForEachAsync(cfs, options, async (cf, token) =>
            {
                try
                {
                    await api.DeleteCustomFormat(cf.Id, token);
                    log.Debug("Deleted {Name}", cf.Name);
                }
                catch (Exception e)
                {
                    log.Debug(e, "Failed to delete CF");
                    console.WriteLine($"Failed to delete CF: {cf.Name}");
                }

                task.Increment(1);
            });
        });
    }

    private async Task<IList<CustomFormatData>> ObtainCustomFormats(ICustomFormatApiService api, CancellationToken ct)
    {
        IList<CustomFormatData> cfs = new List<CustomFormatData>();

        await console.Status().StartAsync("Obtaining custom formats...", async _ =>
        {
            cfs = await api.GetCustomFormats(ct);
        });

        return cfs;
    }

    private IList<CustomFormatData> ProcessManuallySpecifiedFormats(
        IDeleteCustomFormatSettings settings,
        IList<CustomFormatData> cfs)
    {
        ILookup<bool, (string Name, IEnumerable<CustomFormatData> Cfs)> result = settings.CustomFormatNames
            .GroupJoin(cfs,
                x => x,
                x => x.Name,
                (x, y) => (Name: x, Cf: y),
                StringComparer.InvariantCultureIgnoreCase)
            .ToLookup(x => x.Cf.Any());

        // 'false' means there were no CFs matched to this CF name
        if (result[false].Any())
        {
            var cfNames = result[false].Select(x => x.Name).ToList();
            log.Debug("Unmatched CFs: {Names}", cfNames);
            foreach (var name in cfNames)
            {
                console.MarkupLineInterpolated($"[yellow]Warning[/]: Unmatched CF Name: [teal]{name}[/]");
            }
        }

        // 'true' represents CFs that match names provided in user-input (if provided)
        cfs = result[true].SelectMany(x => x.Cfs).ToList();
        return cfs;
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private void PrintPreview(ICollection<CustomFormatData> cfs)
    {
        console.MarkupLine("The following custom formats will be [bold red]DELETED[/]:");
        console.WriteLine();

        var cfNames = cfs
            .Select(x => x.Name)
            .Order(StringComparer.InvariantCultureIgnoreCase)
            .Chunk(Math.Max(15, cfs.Count / 3)) // Minimum row size is 15 for the table
            .ToList();

        var grid = new Grid().AddColumns(cfNames.Count);

        foreach (var rowItems in cfNames.Transpose())
        {
            grid.AddRow(rowItems
                .Select(x => Markup.FromInterpolated($"[bold white]{x}[/]"))
                .ToArray());
        }

        console.Write(grid);
        console.WriteLine();
    }

    private IServiceConfiguration GetTargetConfig(IDeleteCustomFormatSettings settings)
    {
        var configs = configRegistry.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            Instances = new[] {settings.InstanceName}
        });

        switch (configs.Count)
        {
            case 0:
                throw new ArgumentException($"No configuration found with name: {settings.InstanceName}");

            case > 1:
                throw new ArgumentException($"More than one instance found with this name: {settings.InstanceName}");
        }

        return configs.Single();
    }
}
