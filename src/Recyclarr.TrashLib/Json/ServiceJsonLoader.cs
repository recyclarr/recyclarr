using System.IO.Abstractions;
using Newtonsoft.Json;

namespace Recyclarr.TrashLib.Json;

public class ServiceJsonLoader : IBulkJsonLoader
{
    private readonly IBulkJsonLoader _loader;

    public ServiceJsonLoader(Func<JsonSerializerSettings, IBulkJsonLoader> jsonLoaderFactory)
    {
        _loader = jsonLoaderFactory(GlobalJsonSerializerSettings.Services);
    }

    public ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null)
    {
        return _loader.LoadAllFilesAtPaths(jsonPaths, extra);
    }
}
