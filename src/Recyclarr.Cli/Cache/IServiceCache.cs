using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Cache;

public interface IServiceCache
{
    T? Load<T>(IServiceConfiguration config) where T : class;
    void Save<T>(T obj, IServiceConfiguration config) where T : class;
}
