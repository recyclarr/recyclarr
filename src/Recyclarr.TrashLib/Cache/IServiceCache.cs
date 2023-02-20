using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Cache;

public interface IServiceCache
{
    T? Load<T>(IServiceConfiguration config) where T : class;
    void Save<T>(T obj, IServiceConfiguration config) where T : class;
}
