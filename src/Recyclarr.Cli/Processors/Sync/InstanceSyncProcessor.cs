using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.Logging;
using Recyclarr.Sync;
using Serilog.Context;
using Spectre.Console;

namespace Recyclarr.Cli.Processors.Sync;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class InstanceSyncProcessor(
    ILogger log,
    IAnsiConsole console,
    IServiceConfiguration config,
    IInstancePublisher instancePublisher,
    ExceptionHandler exceptionHandler,
    SyncEventOutputStrategy syncEventOutput,
    ServiceAgnosticCapabilityEnforcer enforcer,
    ILifetimeScope instanceScope
)
{
    public async Task<InstanceSyncResult> Process(ISyncSettings settings, CancellationToken ct)
    {
        using var _ = LogContext.PushProperty(LogProperty.Scope, config.InstanceName);

        // Pipelines exist only after detection succeeds; a failed compatibility check has none
        // to mark as interrupted.
        IPipelineExecutor? pipelines = null;

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

            var detected = await enforcer.Check(config, ct);
            using var detectedScope = instanceScope.BeginDetectedServiceScope(detected);
            pipelines = detectedScope.Resolve<IPipelineExecutor>();

            var plan = detectedScope.Resolve<PlanBuilder>().Build();
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

            pipelines?.InterruptAll(instancePublisher);
            return InstanceSyncResult.Failed;
        }
    }
}
