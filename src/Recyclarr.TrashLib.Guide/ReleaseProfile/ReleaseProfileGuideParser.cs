using System.IO.Abstractions;
using Newtonsoft.Json;
using Recyclarr.Common;

namespace Recyclarr.TrashLib.Guide.ReleaseProfile;

public class ReleaseProfileGuideParser
{
    private readonly ILogger _log;

    public ReleaseProfileGuideParser(ILogger log)
    {
        _log = log;
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

    public IEnumerable<ReleaseProfileData> GetReleaseProfileData(IEnumerable<IDirectoryInfo> paths)
    {
        var converter = new TermDataConverter();
        var tasks = JsonUtils.GetJsonFilesInDirectories(paths, _log)
            .Select(x => LoadAndParseFile(x, converter));

        var data = Task.WhenAll(tasks).Result
            // Make non-nullable type and filter out null values
            .Choose(x => x is not null ? (true, x) : default);

        var validator = new ReleaseProfileDataValidationFilterer(_log);
        return validator.FilterProfiles(data);
    }
}
