using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Json;

public interface IBulkJsonLoader
{
    ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null);
}
