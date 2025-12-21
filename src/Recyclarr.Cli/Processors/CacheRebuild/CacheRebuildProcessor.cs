using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.CacheRebuild;

[UsedImplicitly]
internal class CacheRebuildConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public CacheRebuildInstanceProcessor InstanceProcessor { get; } =
        scope.Resolve<CacheRebuildInstanceProcessor>();
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class CacheRebuildProcessor(
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    ConfigurationScopeFactory scopeFactory,
    ExceptionHandler exceptionHandler
)
{
    public async Task<ExitStatus> Process(ICacheRebuildSettings settings, CancellationToken ct)
    {
        // Resource filter: null means "all", otherwise filter to specific type
        // Currently only CustomFormats is implemented
        if (!ShouldRebuildCustomFormats(settings.Resource))
        {
            console.MarkupLine("[yellow]No matching resource types to rebuild.[/]");
            return ExitStatus.Succeeded;
        }

        var configs = configRegistry.FindAndLoadConfigs(
            new ConfigFilterCriteria { Instances = settings.Instances ?? [] }
        );

        if (configs.Count == 0)
        {
            console.MarkupLine("[yellow]No configurations found.[/]");
            return ExitStatus.Succeeded;
        }

        var succeeded = 0;
        var failed = 0;

        foreach (var config in configs)
        {
            try
            {
                using var scope = scopeFactory.Start<CacheRebuildConfigurationScope>(config);
                if (await scope.InstanceProcessor.ProcessAsync(settings, ct))
                {
                    succeeded++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception e)
            {
                if (!await exceptionHandler.TryHandleAsync(e))
                {
                    throw;
                }

                failed++;
            }
        }

        console.WriteLine();
        console.Write(
            new Rule($"[bold]Cache rebuild: {succeeded} succeeded, {failed} failed[/]").RuleStyle(
                "dim"
            )
        );

        return failed > 0 ? ExitStatus.Failed : ExitStatus.Succeeded;
    }

    private static bool ShouldRebuildCustomFormats(CacheableResourceType? filter)
    {
        // null = all resource types, or explicit CustomFormats
        return filter is null or CacheableResourceType.CustomFormats;
    }
}
