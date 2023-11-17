using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Delete;

public class DeleteCustomFormatsProcessor(
    ILogger log,
    IAnsiConsole console,
    ICustomFormatApiService api,
    IConfigurationRegistry configRegistry,
    ISonarrCapabilityFetcher sonarCapabilities)
    : IDeleteCustomFormatsProcessor
{
    public async Task Process(IDeleteCustomFormatSettings settings)
    {
        var config = GetTargetConfig(settings);

        await CheckCustomFormatSupport(config);

        var cfs = await ObtainCustomFormats(config);

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

        await DeleteCustomFormats(cfs, config);
    }

    private async Task CheckCustomFormatSupport(IServiceConfiguration config)
    {
        if (config is SonarrConfiguration)
        {
            var capabilities = await sonarCapabilities.GetCapabilities(config);
            if (!capabilities.SupportsCustomFormats)
            {
                throw new ServiceIncompatibilityException("Custom formats are not supported in Sonarr v3");
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task DeleteCustomFormats(ICollection<CustomFormatData> cfs, IServiceConfiguration config)
    {
        await console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Deleting Custom Formats").MaxValue(cfs.Count);

            var options = new ParallelOptions {MaxDegreeOfParallelism = 8};
            await Parallel.ForEachAsync(cfs, options, async (cf, token) =>
            {
                try
                {
                    await api.DeleteCustomFormat(config, cf.Id, token);
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

    private async Task<IList<CustomFormatData>> ObtainCustomFormats(IServiceConfiguration config)
    {
        IList<CustomFormatData> cfs = new List<CustomFormatData>();

        await console.Status().StartAsync("Obtaining custom formats...", async _ =>
        {
            cfs = await api.GetCustomFormats(config);
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
