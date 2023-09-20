using System.IO.Abstractions;
using System.Text.Json;

namespace Recyclarr.Json.Loading;

public class ServiceJsonLoader : IBulkJsonLoader
{
    private readonly IBulkJsonLoader _loader;

    public ServiceJsonLoader(Func<JsonSerializerOptions, IBulkJsonLoader> jsonLoaderFactory)
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
