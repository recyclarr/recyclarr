using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Pipelines;

// This interface is valuable because it allows having a collection of generic sync pipelines
// without needing to be aware of the generic parameters.
internal interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings, CancellationToken ct);
}
