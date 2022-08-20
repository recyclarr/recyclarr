using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Repo;

namespace TrashLib.Radarr.CustomFormat.Guide;

public class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly IRepoPathsFactory _pathsFactory;
    private readonly ILogger _log;

    public LocalRepoCustomFormatJsonParser(IRepoPathsFactory pathsFactory, ILogger log)
    {
        _pathsFactory = pathsFactory;
        _log = log;
    }

    public ICollection<CustomFormatData> GetCustomFormatData()
    {
        var paths = _pathsFactory.Create();
        var jsonFiles = paths.RadarrCustomFormatPaths
            .SelectMany(x => x.GetFiles("*.json"));

        return jsonFiles.ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile(x)))
            .Merge(8)
            .NotNull()
            .ToEnumerable()
            .ToList();
    }

    private IObservable<CustomFormatData?> LoadJsonFromFile(IFileInfo file)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Do(_ => _log.Debug("Parsing CF Json: {Name}", file.Name))
            .Select(ParseCustomFormatData)
            .Catch((JsonException e) =>
            {
                _log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<CustomFormatData>();
            });
    }

    public static CustomFormatData ParseCustomFormatData(string guideData)
    {
        var obj = JObject.Parse(guideData);

        var name = obj.ValueOrThrow<string>("name");
        var trashId = obj.ValueOrThrow<string>("trash_id");
        int? finalScore = null;

        if (obj.TryGetValue("trash_score", out var score))
        {
            finalScore = (int) score;
            obj.Property("trash_score")?.Remove();
        }

        // Remove trash_id, it's metadata that is not meant for Radarr itself
        // Radarr supposedly drops this anyway, but I prefer it to be removed.
        obj.Property("trash_id")?.Remove();

        return new CustomFormatData(name, trashId, finalScore, obj);
    }
}
