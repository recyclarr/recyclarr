using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.Plan;

namespace Recyclarr.Cli.Pipelines;

// This interface is valuable because it allows having a collection of generic sync pipelines
// without needing to be aware of the generic parameters.
internal interface ISyncPipeline
{
    Task Execute(ISyncSettings settings, PipelinePlan plan, CancellationToken ct);
}
