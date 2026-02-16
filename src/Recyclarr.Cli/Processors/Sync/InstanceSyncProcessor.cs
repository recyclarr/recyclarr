using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Events;
using Serilog.Context;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class InstanceSyncProcessor(
    ILogger log,
    IAnsiConsole console,
    IServiceConfiguration config,
    PlanBuilder planBuilder,
    IPipelineExecutor pipelines,
    ExceptionHandler exceptionHandler,
    SyncEventOutputStrategy syncEventOutput,
    ISyncContextSource contextSource,
    SyncEventStorage eventStorage,
    ServiceAgnosticCapabilityEnforcer enforcer
)
{
    public async Task<InstanceSyncResult> Process(
        ISyncSettings settings,
        InstancePublisher instancePublisher,
        CancellationToken ct
    )
    {
        contextSource.SetInstance(config.InstanceName);
        using var _ = LogContext.PushProperty("InstanceName", config.InstanceName);

        try
        {
            if (settings.Preview)
            {
                console.WriteLine();
                console.Write(new Rule($"[bold]{config.InstanceName}[/]").LeftJustified());
            }

            log.Information(
                "Processing {Server} server {Name}",
                config.ServiceType,
                config.InstanceName
            );

            await enforcer.Check(config, ct);

            var plan = planBuilder.Build();

            if (eventStorage.HasInstanceErrors(config.InstanceName))
            {
                return InstanceSyncResult.Failed;
            }

            var result = await pipelines.Execute(settings, plan, instancePublisher, ct);
            return result == PipelineResult.Failed
                ? InstanceSyncResult.Failed
                : InstanceSyncResult.Succeeded;
        }
        catch (Exception e)
        {
            if (!await exceptionHandler.TryHandleAsync(e, syncEventOutput))
            {
                throw;
            }

            return InstanceSyncResult.Failed;
        }
    }
}
