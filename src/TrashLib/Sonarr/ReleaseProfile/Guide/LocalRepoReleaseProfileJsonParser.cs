using System.IO.Abstractions;
using Common.Extensions;
using Common.FluentValidation;
using MoreLinq;
using Newtonsoft.Json;
using Serilog;

namespace TrashLib.Sonarr.ReleaseProfile.Guide;

public class LocalRepoReleaseProfileJsonParser : ISonarrGuideService
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly ILogger _log;
    private readonly Lazy<IEnumerable<ReleaseProfileData>> _data;

    public LocalRepoReleaseProfileJsonParser(IFileSystem fs, IAppPaths paths, ILogger log)
    {
        _fs = fs;
        _paths = paths;
        _log = log;
        _data = new Lazy<IEnumerable<ReleaseProfileData>>(GetReleaseProfileDataImpl);
    }

    private IEnumerable<ReleaseProfileData> GetReleaseProfileDataImpl()
    {
        var converter = new TermDataConverter();
        var jsonDir = _fs.Path.Combine(_paths.RepoDirectory, "docs/json/sonarr");
        var tasks = _fs.Directory.GetFiles(jsonDir, "*.json")
            .Select(f => LoadAndParseFile(f, converter));

        return Task.WhenAll(tasks).Result
            // Make non-nullable type and filter out null values
            .Choose(x => x is not null ? (true, x) : default);
    }

    private async Task<ReleaseProfileData?> LoadAndParseFile(string file, params JsonConverter[] converters)
    {
        try
        {
            var json = await _fs.File.ReadAllTextAsync(file);
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

    private void HandleJsonException(JsonException exception, string file)
    {
        _log.Warning(exception,
            "Failed to parse Sonarr JSON file (This likely indicates a bug that should be " +
            "reported in the TRaSH repo): {File}", _fs.Path.GetFileName(file));
    }

    public ReleaseProfileData? GetUnfilteredProfileById(string trashId)
    {
        return _data.Value.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData()
    {
        return _data.Value
            .IsValid(new ReleaseProfileDataValidator())
            .ToList();
    }
}
