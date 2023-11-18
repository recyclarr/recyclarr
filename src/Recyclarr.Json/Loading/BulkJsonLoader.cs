using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;

namespace Recyclarr.Json.Loading;

public record LoadedJsonObject<T>(IFileInfo File, T Obj);

public class BulkJsonLoader(ILogger log, JsonSerializerOptions serializerSettings) : IBulkJsonLoader
{
    public ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null)
    {
        var jsonFiles = JsonUtils.GetJsonFilesInDirectories(jsonPaths, log);
        var observable = jsonFiles.ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile<T>(x)))
            .Merge(8);

        var convertedObservable = extra?.Invoke(observable) ?? observable.Select(x => x.Obj);

        return convertedObservable.ToEnumerable().ToList();
    }

    private T ParseJson<T>(string guideData, string fileName)
    {
        var obj = JsonSerializer.Deserialize<T>(guideData, serializerSettings);
        if (obj is null)
        {
            throw new JsonException($"Unable to parse JSON at file {fileName}");
        }

        return obj;
    }

    private IObservable<LoadedJsonObject<T>> LoadJsonFromFile<T>(IFileInfo file)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Select(x => new LoadedJsonObject<T>(file, ParseJson<T>(x, file.Name)))
            .Catch((JsonException e) =>
            {
                log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<LoadedJsonObject<T>>();
            });
    }
}
