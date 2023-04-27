using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Cache;

public interface ICachePersister
{
    CustomFormatCache Load(IServiceConfiguration config);
    void Save(IServiceConfiguration config, CustomFormatCache cache);
}
