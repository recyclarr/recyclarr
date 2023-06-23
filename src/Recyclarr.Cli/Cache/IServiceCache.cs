using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Cache;

public interface IServiceCache
{
    T? Load<T>(IServiceConfiguration config) where T : class;
    void Save<T>(T obj, IServiceConfiguration config) where T : class;
}
