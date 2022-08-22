using System.IO.Abstractions;
using Common.Extensions;
using MoreLinq;
using Newtonsoft.Json;
using Serilog;
using TrashLib.Repo;
using TrashLib.Services.Sonarr.ReleaseProfile.Filters;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Guide;

public class LocalRepoReleaseProfileJsonParser : ISonarrGuideService
{
    private readonly IRepoPathsFactory _pathFactory;
    private readonly ILogger _log;
    private readonly IFileSystem _fs;
    private readonly Lazy<IEnumerable<ReleaseProfileData>> _data;

    public LocalRepoReleaseProfileJsonParser(IRepoPathsFactory pathFactory, ILogger log, IFileSystem fs)
    {
        _pathFactory = pathFactory;
        _log = log;
        _fs = fs;
        _data = new Lazy<IEnumerable<ReleaseProfileData>>(GetReleaseProfileDataImpl);
    }

    private IEnumerable<ReleaseProfileData> GetReleaseProfileDataImpl()
    {
        var converter = new TermDataConverter();
        var paths = _pathFactory.Create();
        var tasks = paths.SonarrReleaseProfilePaths
            .SelectMany(x => x.GetFiles("*.json"))
            .Select(x => LoadAndParseFile(x, converter));

        var data = Task.WhenAll(tasks).Result
            // Make non-nullable type and filter out null values
            .Choose(x => x is not null ? (true, x) : default);

        var validator = new ReleaseProfileDataValidationFilterer(_log);
        return validator.FilterProfiles(data);
    }

    private async Task<ReleaseProfileData?> LoadAndParseFile(IFileInfo file, params JsonConverter[] converters)
    {
        try
        {
            using var stream = file.OpenText();
            var json = await stream.ReadToEndAsync();
            return JsonConvert.DeserializeObject<ReleaseProfileData>(json, converters);
        }
        catch (JsonException e)
        {
            HandleJsonException(e, file);
        }
        catch (AggregateException ae) when (ae.InnerException is JsonException e)
        {
            HandleJsonException(e, file);
        }

        return null;
    }

    private void HandleJsonException(JsonException exception, IFileInfo file)
    {
        _log.Warning(exception,
            "Failed to parse Sonarr JSON file (This likely indicates a bug that should be " +
            "reported in the TRaSH repo): {File}", file.Name);
    }

    public ReleaseProfileData? GetUnfilteredProfileById(string trashId)
    {
        return _data.Value.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData()
    {
        return _data.Value.ToList();
    }
}
