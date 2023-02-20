using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Pipelines;

public interface ISyncPipeline
{
    public Task Execute(ISyncSettings settings, IServiceConfiguration config);
}
