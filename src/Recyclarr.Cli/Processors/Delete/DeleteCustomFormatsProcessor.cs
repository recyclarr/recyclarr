using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.CustomFormat.Api;
using Recyclarr.TrashLib.Compatibility.Sonarr;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Models;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Delete;

public class DeleteCustomFormatsProcessor : IDeleteCustomFormatsProcessor
{
    private readonly ILogger _log;
    private readonly IAnsiConsole _console;
    private readonly ICustomFormatService _api;
    private readonly IConfigurationRegistry _configRegistry;
    private readonly ISonarrCapabilityFetcher _sonarCapabilities;

    public DeleteCustomFormatsProcessor(
        ILogger log,
        IAnsiConsole console,
        ICustomFormatService api,
        IConfigurationRegistry configRegistry,
        ISonarrCapabilityFetcher sonarCapabilities)
    {
        _log = log;
        _console = console;
        _api = api;
        _configRegistry = configRegistry;
        _sonarCapabilities = sonarCapabilities;
    }

    public async Task Process(IDeleteCustomFormatSettings settings)
    {
        var config = GetTargetConfig(settings);

        await CheckCustomFormatSupport(config);

        var cfs = await ObtainCustomFormats(config);

        if (!settings.All)
        {
            if (!settings.CustomFormatNames.Any())
            {
                throw new CommandException("Custom format names must be specified if the `--all` option is not used.");
            }

            cfs = ProcessManuallySpecifiedFormats(settings, cfs);
        }

        if (!cfs.Any())
        {
            _console.MarkupLine("[yellow]Done[/]: No custom formats found or specified to delete.");
            return;
        }

        PrintPreview(cfs);

        if (settings.Preview)
        {
            _console.MarkupLine("This is a preview! [u]No actual deletions will be performed.[/]");
            return;
        }

        if (!settings.Force &&
            !_console.Confirm("\nAre you sure you want to [bold red]permanently delete[/] the above custom formats?"))
        {
            _console.WriteLine("Aborted!");
            return;
        }

        await DeleteCustomFormats(cfs, config);
    }

    private async Task CheckCustomFormatSupport(IServiceConfiguration config)
    {
        if (config is SonarrConfiguration)
        {
            var capabilities = await _sonarCapabilities.GetCapabilities(config);
            if (!capabilities.SupportsCustomFormats)
            {
                throw new ServiceIncompatibilityException("Custom formats are not supported in Sonarr v3");
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task DeleteCustomFormats(ICollection<CustomFormatData> cfs, IServiceConfiguration config)
    {
        await _console.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Deleting Custom Formats").MaxValue(cfs.Count);

            var options = new ParallelOptions {MaxDegreeOfParallelism = 8};
            await Parallel.ForEachAsync(cfs, options, async (cf, token) =>
            {
                try
                {
                    await _api.DeleteCustomFormat(config, cf.Id, token);
                    _log.Debug("Deleted {Name}", cf.Name);
                }
                catch (Exception e)
                {
                    _log.Debug(e, "Failed to delete CF");
                    _console.WriteLine($"Failed to delete CF: {cf.Name}");
                }

                task.Increment(1);
            });
        });
    }

    private async Task<IList<CustomFormatData>> ObtainCustomFormats(IServiceConfiguration config)
    {
        IList<CustomFormatData> cfs = new List<CustomFormatData>();

        await _console.Status().StartAsync("Obtaining custom formats...", async _ =>
        {
            cfs = await _api.GetCustomFormats(config);
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
            _log.Debug("Unmatched CFs: {Names}", cfNames);
            foreach (var name in cfNames)
            {
                _console.MarkupLineInterpolated($"[yellow]Warning[/]: Unmatched CF Name: [teal]{name}[/]");
            }
        }

        // 'true' represents CFs that match names provided in user-input (if provided)
        cfs = result[true].SelectMany(x => x.Cfs).ToList();
        return cfs;
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private void PrintPreview(ICollection<CustomFormatData> cfs)
    {
        _console.MarkupLine("The following custom formats will be [bold red]DELETED[/]:");
        _console.WriteLine();

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

        _console.Write(grid);
        _console.WriteLine();
    }

    private IServiceConfiguration GetTargetConfig(IDeleteCustomFormatSettings settings)
    {
        var configs = _configRegistry.FindAndLoadConfigs(new ConfigFilterCriteria
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
