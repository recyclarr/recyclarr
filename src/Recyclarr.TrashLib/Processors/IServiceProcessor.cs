using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Processors;

public interface IServiceProcessor
{
    Task Process(ISyncSettings settings, IServiceConfiguration config);
}
