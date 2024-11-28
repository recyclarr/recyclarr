using System.IO.Abstractions;

namespace Recyclarr.Json.Loading;

public interface IBulkJsonLoader
{
    ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null
    );
}
