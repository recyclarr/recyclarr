using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Services.Processors;

public interface IServiceProcessor<T> where T : ServiceConfiguration
{
    Task Process(T config, ISyncSettings settings);
}
