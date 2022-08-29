using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TrashLib.Repo;
using TrashLib.Services.Common.QualityDefinition;
using TrashLib.Services.Radarr.CustomFormat.Models;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Services.Radarr.CustomFormat.Guide;

public class LocalRepoRadarrGuideService : IRadarrGuideService
{
    private readonly IRepoPathsFactory _pathsFactory;
    private readonly ILogger _log;
    private readonly QualityGuideParser<RadarrQualityData> _parser;

    public LocalRepoRadarrGuideService(IRepoPathsFactory pathsFactory, ILogger log)
    {
        _pathsFactory = pathsFactory;
        _log = log;
        _parser = new QualityGuideParser<RadarrQualityData>(log);
    }

    public ICollection<RadarrQualityData> GetQualities()
        => _parser.GetQualities(_pathsFactory.Create().RadarrQualityPaths);

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
        }

        // Remove any properties starting with "trash_". Those are metadata that are not meant for Radarr itself Radarr
        // supposedly drops this anyway, but I prefer it to be removed. ToList() is important here since removing the
        // property itself modifies the collection, and we don't want the collection to get modified while still looping
        // over it.
        foreach (var trashProperty in obj.Properties().Where(x => Regex.IsMatch(x.Name, @"^trash_")).ToList())
        {
            trashProperty.Remove();
        }

        return new CustomFormatData(name, trashId, finalScore, obj);
    }
}
