using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using Recyclarr.Common;

namespace Recyclarr.TrashLib.Json;

public record LoadedJsonObject<T>(IFileInfo File, T Obj);

public class BulkJsonLoader
{
    private readonly ILogger _log;

    public BulkJsonLoader(ILogger log)
    {
        _log = log;
    }

    public ICollection<T> LoadAllFilesAtPaths<T>(
        IEnumerable<IDirectoryInfo> jsonPaths,
        Func<IObservable<LoadedJsonObject<T>>, IObservable<T>>? extra = null)
    {
        var jsonFiles = JsonUtils.GetJsonFilesInDirectories(jsonPaths, _log);
        var observable = jsonFiles.ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile<T>(x)))
            .Merge(8);

        var convertedObservable = extra?.Invoke(observable) ?? observable.Select(x => x.Obj);

        return convertedObservable.ToEnumerable().ToList();
    }

    private static T ParseJson<T>(string guideData, string fileName)
    {
        var obj = JsonConvert.DeserializeObject<T>(guideData, GlobalJsonSerializerSettings.Guide);
        if (obj is null)
        {
            throw new JsonSerializationException($"Unable to parse JSON at file {fileName}");
        }

        return obj;
    }

    private IObservable<LoadedJsonObject<T>> LoadJsonFromFile<T>(IFileInfo file)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Select(x => new LoadedJsonObject<T>(file, ParseJson<T>(x, file.Name)))
            .Catch((JsonException e) =>
            {
                _log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<LoadedJsonObject<T>>();
            });
    }
}
