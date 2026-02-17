using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Models;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.StateRepair;

[UsedImplicitly]
internal class CacheRebuildConfigurationScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public StateRepairInstanceProcessor InstanceProcessor { get; } =
        scope.Resolve<StateRepairInstanceProcessor>();
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class StateRepairProcessor(
    IAnsiConsole console,
    ConfigurationRegistry configRegistry,
    LifetimeScopeFactory scopeFactory,
    ExceptionHandler exceptionHandler
)
{
    public async Task<ExitStatus> Process(IStateRepairSettings settings, CancellationToken ct)
    {
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
                using var scope = scopeFactory.Start<CacheRebuildConfigurationScope>(c =>
                {
                    c.RegisterInstance(config).As(config.GetType()).As<IServiceConfiguration>();
                    c.RegisterType<CacheRebuildConfigurationScope>();
                });
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
            new Rule($"[bold]State repair: {succeeded} succeeded, {failed} failed[/]").RuleStyle(
                "dim"
            )
        );

        return failed > 0 ? ExitStatus.Failed : ExitStatus.Succeeded;
    }
}
