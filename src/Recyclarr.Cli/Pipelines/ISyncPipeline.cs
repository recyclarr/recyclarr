using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines;

public interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings, IServiceConfiguration config);
}
