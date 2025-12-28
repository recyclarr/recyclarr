using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;

namespace Recyclarr.Cli.Pipelines;

internal interface IPipelineExecutor
{
    Task<PipelineResult> Execute(ISyncSettings settings, PipelinePlan plan, CancellationToken ct);
}
