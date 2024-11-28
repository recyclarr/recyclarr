using System.IO.Abstractions;
using System.Text.Json;

namespace Recyclarr.Json.Loading;

public class GuideJsonLoader(Func<JsonSerializerOptions, IBulkJsonLoader> jsonLoaderFactory)
    : IBulkJsonLoader
{
    private readonly IBulkJsonLoader _loader = jsonLoaderFactory(
        GlobalJsonSerializerSettings.Guide
    );

    public ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null
    )
    {
        return _loader.LoadAllFilesAtPaths(jsonPaths, extra);
    }
}
