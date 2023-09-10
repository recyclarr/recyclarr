using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines;

public interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings, IServiceConfiguration config);
}
