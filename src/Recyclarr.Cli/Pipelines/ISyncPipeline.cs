using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Pipelines;

public interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings);
}
