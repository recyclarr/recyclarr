using System.IO.Abstractions;
using MoreLinq;
using Newtonsoft.Json;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.QualitySize.Guide;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Filters;
using Serilog;

namespace Recyclarr.TrashLib.Services.Sonarr;

public class LocalRepoSonarrGuideService : SonarrGuideService
{
    private readonly IRepoPathsFactory _pathsFactory;
    private readonly ILogger _log;
    private readonly ICustomFormatLoader _cfLoader;
    private readonly Lazy<IEnumerable<ReleaseProfileData>> _data;
    private readonly QualitySizeGuideParser<QualitySizeData> _parser;

    public LocalRepoSonarrGuideService(
        IRepoPathsFactory pathsFactory,
        ILogger log,
        ICustomFormatLoader cfLoader)
    {
        _pathsFactory = pathsFactory;
        _log = log;
        _cfLoader = cfLoader;
        _data = new Lazy<IEnumerable<ReleaseProfileData>>(GetReleaseProfileDataImpl);
        _parser = new QualitySizeGuideParser<QualitySizeData>(log);
    }

    public override ICollection<QualitySizeData> GetQualities()
    {
        return _parser.GetQualities(_pathsFactory.Create().SonarrQualityPaths);
    }

    public override ICollection<CustomFormatData> GetCustomFormatData()
    {
        var paths = _pathsFactory.Create();
        return _cfLoader.LoadAllCustomFormatsAtPaths(
            paths.SonarrCustomFormatPaths,
            paths.SonarrCollectionOfCustomFormats);
    }

    private IEnumerable<ReleaseProfileData> GetReleaseProfileDataImpl()
    {
        var converter = new TermDataConverter();
        var paths = _pathsFactory.Create();
        var tasks = JsonUtils.GetJsonFilesInDirectories(paths.SonarrReleaseProfilePaths, _log)
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

    public override ReleaseProfileData? GetUnfilteredProfileById(string trashId)
    {
        return _data.Value.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public override IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData()
    {
        return _data.Value.ToList();
    }
}
