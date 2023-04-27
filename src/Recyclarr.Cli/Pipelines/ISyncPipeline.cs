using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines;

public interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings, IServiceConfiguration config);
}
