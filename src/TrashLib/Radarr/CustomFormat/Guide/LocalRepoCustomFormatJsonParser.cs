using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Startup;

namespace TrashLib.Radarr.CustomFormat.Guide;

public class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly IAppPaths _paths;
    private readonly ILogger _log;
    private readonly IFileSystem _fs;

    public LocalRepoCustomFormatJsonParser(IAppPaths paths, ILogger log, IFileSystem fs)
    {
        _paths = paths;
        _log = log;
        _fs = fs;
    }

    public IObservable<CustomFormatData> GetCustomFormatData()
    {
        var jsonDir = _paths.RepoDirectory
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory("radarr");

        return jsonDir.EnumerateFiles("*.json").ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile(x)))
            .Merge(8)
            .NotNull();
    }

    private IObservable<CustomFormatData?> LoadJsonFromFile(IFileInfo file)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Do(_ => _log.Debug("Parsing CF Json: {Name}", file.Name))
            .Select(x => ParseCustomFormatData(x, _fs.Path.GetFileNameWithoutExtension(file.Name)))
            .Catch((JsonException e) =>
            {
                _log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<CustomFormatData>();
            });
    }

    public static CustomFormatData ParseCustomFormatData(string guideData, string fileName)
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

        return new CustomFormatData(name, fileName, trashId, finalScore, obj);
    }
}
